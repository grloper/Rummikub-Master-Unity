using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public float computerMoveDelay = 0.5f;
    // Player reference
    private Player myPlayer;

    private List<CardsSet> ExtractMaxValidRunSets(List<Card> computerHand)
    {
        SortByRun();
        //i want this function to return from 13 cards as a set and go down to 1 and if it can't find a set of 13 cards it will return a set of 12 cards and so on
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(computerHand[0]);
        for (int i = 1; i < computerHand.Count; i++)
        {
            if (computerHand[i].Number == computerHand[i - 1].Number + 1
                && computerHand[i].Color == computerHand[i - 1].Color)
            {
                currentSet.AddCardToEnd(computerHand[i]);

            }
            else
            {
                if (currentSet.set.Count > 2)
                {
                    cardsSets.Add(currentSet);
                }
                currentSet = new CardsSet();
                currentSet.AddCardToEnd(computerHand[i]);
            }
        }
        return cardsSets;
    }

    private List<CardsSet> ExtractMaxValidGroupSets(List<Card> computerHand)
    {
        SortByGroup();
        List<CardsSet> cardsSets = new List<CardsSet>();

        for (int i = 0; i < computerHand.Count; i++)
        {
            CardsSet currentSet = new CardsSet();
            currentSet.AddCardToEnd(computerHand[i]);

            for (int j = i + 1; j < computerHand.Count; j++)
            {
                if (computerHand[j].Number == computerHand[i].Number &&
                    !currentSet.set.Any(card => card.Color == computerHand[j].Color))
                {
                    currentSet.AddCardToEnd(computerHand[j]);
                }
                else
                {
                    break;
                }
            }

            if (currentSet.set.Count >= 3)
            {
                cardsSets.Add(currentSet);
            }

            i += currentSet.set.Count - 1; // Skip processed cards
        }

        return cardsSets;
    }

    private void MaximizeDrops()
    {
        computerHand = myPlayer.GetPlayerHand();

        // Extract sets for the first order of calls
        List<CardsSet> cardsSetsValueFirst = ExtractMaxValidRunSets(computerHand);
        List<CardsSet> cardsSetsGroupFirst = ExtractMaxValidGroupSets(computerHand);

        int totalCardsInSetsFirst = GetTotalCardsInSets(cardsSetsValueFirst) + GetTotalCardsInSets(cardsSetsGroupFirst);
        if (totalCardsInSetsFirst == 0)
        {
            uiManager.DrawACardFromDeck();
        }
        computerHand = myPlayer.GetPlayerHand();
        // Extract sets for the second order of calls
        List<CardsSet> cardsSetsGroupSecond = ExtractMaxValidGroupSets(computerHand);
        List<CardsSet> cardsSetsValueSecond = ExtractMaxValidRunSets(computerHand);

        int totalCardsInSetsSecond = GetTotalCardsInSets(cardsSetsValueSecond) + GetTotalCardsInSets(cardsSetsGroupSecond);

        // Compare the totals
        if (totalCardsInSetsFirst >= totalCardsInSetsSecond)
        {
            // First order has more cards
            if (cardsSetsValueFirst.Count > 0)
            {

                for (int i = 0; i < cardsSetsValueFirst.Count; i++)
                {
                    gameBoard.PlayCardSetOnBoard(cardsSetsValueFirst[i]);
                }
            }
            if (cardsSetsGroupFirst.Count > 0)
            {

                for (int i = 0; i < cardsSetsGroupFirst.Count; i++)
                {
                    gameBoard.PlayCardSetOnBoard(cardsSetsGroupFirst[i]);
                }
            }
            //    uiManager.ConfirmMove();


        }
        else if (totalCardsInSetsSecond > totalCardsInSetsFirst)
        {
            // Second order has more cards
            if (cardsSetsValueSecond.Count > 0)
            {

                for (int i = 0; i < cardsSetsValueSecond.Count; i++)
                {
                    gameBoard.PlayCardSetOnBoard(cardsSetsValueSecond[i]);
                }
            }
            if (cardsSetsGroupSecond.Count > 0)
            {

                for (int i = 0; i < cardsSetsGroupSecond.Count; i++)
                {
                    gameBoard.PlayCardSetOnBoard(cardsSetsGroupSecond[i]);
                }
            }
            //  uiManager.ConfirmMove();
        }

    }

    // Method to calculate the total number of cards in a list of sets
    private int GetTotalCardsInSets(List<CardsSet> sets)
    {
        int total = 0;
        foreach (CardsSet set in sets)
        {
            total += set.set.Count;
        }
        return total;
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
        print("TURN");
        //1. Maximize the number of cards dropped on the board
        MaximizeDrops();
  
        // 2. use partial sets
        // 3. extract free cards and use them to complete partial sets
        // 4. draw a card from the deck

    }


    public void SortByGroup()
    {
        // Sort the cards by group
        computerHand.Sort((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Color.CompareTo(card2.Color);
            else
                return card1.Number.CompareTo(card2.Number);
        });
    }
    public void SortByRun()
    {
        computerHand.Sort((card1, card2) =>
        {
            if (card1.Color == card2.Color)
                return card1.Number.CompareTo(card2.Number);
            else
                return card1.Color.CompareTo(card2.Color);
        });
    }
}