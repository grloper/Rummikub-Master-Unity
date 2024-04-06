using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.Unicode;

public class Computer : Player
{

    // game board reference
    [HideInInspector] private GameBoard gameBoard;
    // Reference to the ui manager 
    [HideInInspector] private UImanager uiManager;
    // Reference to the game controller
    [HideInInspector] private GameController gameController;
    // The delay for the computer move
    List<Card> computerHand = new List<Card>();
    public float computerMoveDelay = 2f;
    // Player reference
    private Player myPlayer;

    private List<CardsSet> ExtractMaxValidRunSets(List<Card> list)
    {
        SortByRun(list);
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].Number == list[i - 1].Number + 1
                && list[i].Color == list[i - 1].Color)
            {
                currentSet.AddCardToEnd(list[i]);
            }
            else
            {
                if (currentSet.set.Count >= Constants.MinInRun)
                {
                    cardsSets.Add(currentSet);
                }
                currentSet = new CardsSet();
                currentSet.AddCardToEnd(list[i]);
            }
        }
        return cardsSets;
    }

    private List<CardsSet> ExtractMaxValidGroupSets(List<Card> list)
    {
        SortByGroup(list);
        List<CardsSet> cardsSets = new List<CardsSet>();
        bool found = false;
        for (int i = 0; i < list.Count; i++)
        {
            CardsSet currentSet = new CardsSet();
            currentSet.AddCardToEnd(list[i]);

            for (int j = i + 1; j < list.Count&&!found; j++)
            {
                if (list[j].Number == list[i].Number &&
                    !currentSet.set.Any(card => card.Color == list[j].Color))
                {
                    currentSet.AddCardToEnd(list[j]);
                }
                else
                {
                    found =true;
                }
            }
             //we are axxepting only sets with thelength of 3 to 4
            if (currentSet.set.Count >= Constants.MinInGroup &&
                currentSet.set.Count <= Constants.MaxInGroup)
            {
                cardsSets.Add(currentSet);
            }

            i += currentSet.set.Count - 1; // Skip processed cards
        }

        return cardsSets;
    }
    //private void MaximizeDrops()
    //{
    //    List<Card> handCopy = new List<Card>(computerHand); // Clone the list

    //    // Extract valid run sets
    //    List<CardsSet> cardsSetsRun = ExtractMaxValidRunSets(handCopy);
    //    RemovePlayedCards(handCopy, cardsSetsRun); // Remove cards played in runs

    //    // Extract valid group sets
    //    List<CardsSet> cardsSetsGroup = ExtractMaxValidGroupSets(handCopy);
    //    RemovePlayedCards(handCopy, cardsSetsGroup); // Remove cards played in groups

    //    int totalCardsInSets = GetTotalCardsInSets(cardsSetsRun) + GetTotalCardsInSets(cardsSetsGroup);

    //    if (!myPlayer.GetInitialMove())
    //    {
    //        int totalCardsValueInSets = GetTotalCardsValueInSets(cardsSetsRun) + GetTotalCardsValueInSets(cardsSetsGroup);
    //        if (totalCardsValueInSets < 30)
    //        {
    //            print("1. drawed for " + gameController.GetCurrentPlayerIndex());
    //            uiManager.DrawACardFromDeck();
    //        }
    //        else
    //        {
    //            myPlayer.SetInitialMove(true);
    //        }
    //    }

    //    if (totalCardsInSets > 0)
    //    {
    //        // Play run sets
    //        foreach (CardsSet set in cardsSetsRun)
    //        {
    //            gameBoard.PlayCardSetOnBoard(set);
    //        }

    //        // Play group sets
    //        foreach (CardsSet set in cardsSetsGroup)
    //        {
    //            gameBoard.PlayCardSetOnBoard(set);
    //        }

    //        uiManager.ConfirmMove();
    //    }
    //    else
    //    {
    //        print("2. drawed for " + gameController.GetCurrentPlayerIndex());
    //        uiManager.DrawACardFromDeck();
    //    }
    //}
    private void MaximizeDrops()
    {
        List<Card> handCopy = new List<Card>(computerHand); // Clone the list
        List<Card> removedCards = new List<Card>(); // Track used cards

        // Extract runs with separate removal tracking
        List<CardsSet> runs = ExtractMaxValidRunSets(handCopy);
        foreach (CardsSet run in runs)
        {
            removedCards.AddRange(run.set); // Add cards in the run to removed list
        }

        // Extract groups with separate removal tracking
        List<CardsSet> groups = ExtractMaxValidGroupSets(handCopy);
        foreach (CardsSet group in groups)
        {
            removedCards.AddRange(group.set); // Add cards in the group to removed list
        }

        // Calculate total cards in runs and groups (considering removed)
        int totalCardsInRuns = 0;
        foreach (CardsSet run in runs)
        {
            totalCardsInRuns += run.set.Count;
        }

        int totalCardsInGroups = 0;
        foreach (CardsSet group in groups)
        {
            totalCardsInGroups += group.set.Count;
        }

        // Choose combination with higher total card count
        List<CardsSet> setsToPlay;
        if (totalCardsInRuns + totalCardsInGroups > totalCardsInGroups + totalCardsInRuns)
        {
            setsToPlay = runs.Concat(groups).ToList(); // Combine sets (runs first)
        }
        else
        {
            setsToPlay = groups.Concat(runs).ToList(); // Combine sets (groups first)
        }

        // Play chosen sets (don't remove cards from original hand)
        foreach (CardsSet set in setsToPlay)
        {
            gameBoard.PlayCardSetOnBoard(set);
        }

        // If no sets played, draw a card
        if (setsToPlay.Count == 0)
        {
            print("2. drawed for " + gameController.GetCurrentPlayerIndex());
            uiManager.DrawACardFromDeck();
        }
    }



    public void Initialize(Player player)
    {
        this.myPlayer = player;
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
        this.uiManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UImanager>();
        this.gameBoard = GameObject.FindGameObjectWithTag("BoardGrid").GetComponent<GameBoard>();
    }

    public IEnumerator ComputerMove()
    {
        yield return new WaitForSeconds(computerMoveDelay); // wait for the delay
        // Call the method inside Computer.cs named "DoComputerMove"
        DoComputerMove();

    }

    private void DoComputerMove()
    {
        this.computerHand = myPlayer.GetPlayerHand();
        print("Computer move for " + gameController.GetCurrentPlayerIndex());
        // computerHand = myPlayer.GetPlayerHand();
        if (!myPlayer.GetInitialMove())
        {
            List<Card> pl =  GetInitialMoveGreaterThanThirty(this.computerHand);
            CardsSet set = new CardsSet();
            set.SetList(pl);
            gameBoard.PlayCardSetOnBoard(set);
        }
        else
        {

            //1. Maximize the number of cards dropped on the board
            //MaximizeDrops();

            // 2. use partial sets
            // 3. extract free cards and use them to complete partial sets
            // 4. draw a card from the deck
        }
    }

    public List<Card> GetInitialMoveGreaterThanThirty(List<Card> hand)
    {
        // Iterate through cards in hand
        foreach (Card card in hand)
        {
            // Check for Groups (size 3 or 4)
            List<Card> group = FindGroupInHand(card, hand);
            if (group != null && group.Sum(c => c.Number) > 30)
            {
                return group; // Valid group found, return it
            }

            // Check for Runs (extending existing runs)
            List<Card> run = ExtendRunInHand(card, hand);
            if (run != null && run.Sum(c => c.Number) > 30)
            {
                return run; // Valid run found, return it
            }
        }

        // No move found exceeding 30 points
        return null;
    }

    private List<Card> FindGroupInHand(Card card, List<Card> hand)
    {
        // Filter cards with the same number as the current card
        var potentialGroup = hand.Where(c => c.Number == card.Number && c != card).ToList();

        // Check if a group of size 3 or 4 can be formed with different colors
        if (potentialGroup.Count >= 2)
        {
            // Check for group of size 3 (2 different colors)
            var distinctColors = potentialGroup.Select(c => c.Color).Distinct().ToList();
            if (distinctColors.Count == 2)
            {
                return new List<Card>() { card }.Concat(potentialGroup).ToList();
            }

            // Check for group of size 4 (all 3 colors)
            if (distinctColors.Count == 3 && hand.Any(c => c.Color != card.Color && c.Number == card.Number))
            {
                return new List<Card>() { card }.Concat(potentialGroup).ToList();
            }
        }

        return null; // No valid group found
    }
    private List<Card> ExtendRunInHand(Card card, List<Card> hand)
    {
        // Filter cards with the same color as the current card
        var potentialRun = hand.Where(c => c.Color == card.Color).ToList();

        // Check if cards exist for extending a run before or after the current card
        int runLength = 1;
        int valueToCheck = card.Number - 1; // Check for previous consecutive numbers

        // Look for consecutive cards before the current card
        while (valueToCheck > 0 && potentialRun.Any(c => c.Number == valueToCheck))
        {
            runLength++;
            valueToCheck--;
        }

        valueToCheck = card.Number + 1; // Check for consecutive numbers after the current card

        // Look for consecutive cards after the current card
        while (valueToCheck <= 13 && potentialRun.Any(c => c.Number == valueToCheck))
        {
            runLength++;
            valueToCheck++;
        }

        // Check if a valid run exceeding 30 points is formed
        if (runLength >= 3 && card.Number + runLength - 1 > 30)
        {
            // Extract the cards forming the run
            List<Card> runCards = new List<Card>();
            valueToCheck = card.Number - runLength + 1;
            for (int i = 0; i < runLength; i++)
            {
                runCards.Add(potentialRun.First(c => c.Number == valueToCheck));
                valueToCheck++;
            }
            return runCards;
        }

        return null; // No valid run found exceeding 30 points
    }

    public void SortByGroup(List<Card> list)
    {
        // Sort the cards by group
        list.Sort((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Color.CompareTo(card2.Color);
            else
                return card1.Number.CompareTo(card2.Number);
        });

    }




    public void SortByRun(List<Card> list)
    {
        list.Sort((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Number.CompareTo(card2.Number);
            else
                return card1.Color.CompareTo(card2.Color);
        });
    }


}