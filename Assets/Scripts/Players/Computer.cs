using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
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
    public float computerMoveDelay = 0f;// 0.9f;
    // Player reference
    private Player myPlayer;
    private bool added;
    private bool dropped;

    private List<CardsSet> ExtractMaxValidRunSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        SortByRun();
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            // order of sorted list: 1,2,3,3,1,2
            // if the same card appears
            if (list[i].Number == list[i - 1].Number &&
                list[i].Color == list[i - 1].Color)
            {
                list.Add(list[i]);
                list.Remove(list[i]);
                // remove the card and add it at the end
            }
            if (list[i].Number == list[i - 1].Number + 1
             && list[i].Color == list[i - 1].Color
             && currentSet.set.Count <= maxRangeInclusive)
            {
                currentSet.AddCardToEnd(list[i]);
            }
            else
            {
                if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive)
                {
                    cardsSets.Add(currentSet);
                }
                currentSet = new CardsSet();
                currentSet.AddCardToEnd(list[i]);
            }
        }
        return cardsSets;
    }
    private List<CardsSet> ExtractMaxValidGroupSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        SortByGroup();
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].Number == list[i - 1].Number &&
                list[i].Color == list[i - 1].Color)
            {
                list.Add(list[i]);
                list.Remove(list[i]);
            }
            if (list[i].Number == list[i - 1].Number
                && !currentSet.IsContainThisColor(list[i].Color)
                 && currentSet.set.Count <= maxRangeInclusive)
            {
                currentSet.AddCardToEnd(list[i]);
            }

            else
            {
                if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive)
                {
                    cardsSets.Add(currentSet);
                }
                currentSet = new CardsSet();
                currentSet.AddCardToEnd(list[i]);
            }
        }
        return cardsSets;
    }


    public void MaximizeValidDrops()
    {
        this.dropped = false;
        List<CardsSet> setsRun = ExtractMaxValidRunSets(myPlayer.GetPlayerHand(), Constants.MinInRun, Constants.MaxInRun);
        if (setsRun.Count > 0)
        {
            this.dropped = true;
            foreach (CardsSet set in setsRun)
            {
                gameBoard.PlayCardSetOnBoard(set);
            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(myPlayer.GetPlayerHand(), Constants.MinInGroup, Constants.MaxInGroup);
        if (setsGroup.Count > 0)
        {
            this.dropped = true;
            foreach (CardsSet set in setsGroup)
            {
                gameBoard.PlayCardSetOnBoard(set);
            }
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
        bool allowed = true;
        MaximizeValidDrops();
        if (myPlayer.GetInitialMove()) //if the player is allowed to append cards to the board
        {
            //MaximizePartialDrops();
            AssignFreeCardsToExistsSets();
        }
        else//if the player is not allowed to append cards to the board
        {
            if (gameBoard.GetMovesStackSum() < Constants.MinFirstSet) //he must drop more than 30 in sets
            {
                allowed = false;
            }
        }
        if ((dropped || added) && allowed) //if the computer has made a move, or have sets to drop.
        {
            // if allowed = false => the computer might have valid sets to drop but the sum of the cards in the board is less than 30 and he has not dropped any cards yet.
            uiManager.ConfirmMove();
        }
        else
        {
            uiManager.DrawACardFromDeck();
        }
    }



    /// <summary>
    /// Assigns free cards to existing sets on the game board, rearrange visualy if needed.
    /// </summary>
    private void AssignFreeCardsToExistsSets()
    {
        this.added = false;
        // track the cards that need to be removed from the computer hand because 
        // cant remove them while iterating over the list
        List<Card> cardsToRemove = new List<Card>();
        List<Card> cardsToUpdateFromPlayer = new List<Card>();
        List<Card> cardsToUpdateFromBoard = new List<Card>();

        // iterate over the computer hand
        foreach (Card card in myPlayer.GetPlayerHand())
        {
            // if added a card to a set then break the loop
            bool found = false;
            int offset =-1;//the offset of the card if can be added in the middle of some set and split it into two different sets
            // extract the keys from the dictionary to iterate over them
            List<SetPosition> keys = gameBoard.board.GetGameBoardValidSetsTable().Keys.ToList();
            keys.ToArray();
            // Create a copy of the keys
            for (int i = 0; i < keys.Count && !found; i++) // iterate over the keys (SetPosition objects) until we find a set to add the card to for some card
            {
                // get the set from the dictionary
                SetPosition key = keys[i];
                // get the current set from the dictionary using the SetPosition key
                CardsSet set = gameBoard.board.GetGameBoardValidSetsTable()[key];
                // check if the card can be added to the set without needing to move it
                // if we keeping a set valid then we need to update the keys in the dictionary and add the card
                if (set.CanAddCardEndRun(card) || set.CanAddCardEndGroup(card))
                {
                    found = true; // break the loop
                    this.added = true;
                    cardsToRemove.Add(card);
                    // if there is space for the card then add it to the set
                    // if there is no space for the card then we need to rearrange the set
                    // forward true means that we need to add the card to the end of the set
                    if (set.IsSpaceForCard(true, gameBoard))
                    {
                  
                        card.OldPosition = card.Position;
                        card.Position.SetTileSlot(set.GetLastCard().Position.GetTileSlot() + 1);
                        gameBoard.PlayCardOnBoard(card, set.GetLastCard().Position.GetTileSlot() + 1, false);
                    }
                    else
                    {
                        print("No Space for: " + card.ToString() + " in set: " + set.ToString());
                        // if there is no space for the card then we need to rearrange the set
                        // forward true means that we need to add the card to the end of the set
                        this.gameBoard.RearrangeCardsSet(key, card, true);
                        cardsToUpdateFromPlayer.Add(card);
                        cardsToUpdateFromBoard.AddRange(set.set);
                    }

                }
                else if (set.CanAddCardBegginingRun(card) || set.CanAddCardBegginingGroup(card))
                {
                    found = true; // break the loop
                    this.added = true;
                    cardsToRemove.Add(card);

                    // if there is space for the card then add it to the set
                    // if there is no space for the card then we need to rearrange the set
                    // forward false means that we need to add the card to the beginning of the set
                    if (set.IsSpaceForCard(false, gameBoard))
                    {
                        //save the old position of the card
                        card.OldPosition = card.Position;
                        card.Position.SetTileSlot(set.GetFirstCard().Position.GetTileSlot() - 1);
                        //play the card on the new position
                        gameBoard.PlayCardOnBoard(card, set.GetFirstCard().Position.GetTileSlot() - 1, false);
                    }
                    else
                    {
                        print("No Space for: " + card.ToString() + " in set: " + set.ToString());
                        // if there is no space for the card then we need to rearrange the set
                        // forward false means that we need to add   the card to the beginning of the set
                        this.gameBoard.RearrangeCardsSet(key, card, false);
                        cardsToUpdateFromPlayer.Add(card);
                        cardsToUpdateFromBoard.AddRange(set.set);
                    }
                }
                offset =set.CanAddCardMiddleRun(card);
                if(offset!=-1)
                {
                    found = true;
                    this.added = true;
                    cardsToRemove.Add(card);
                    card.OldPosition = card.Position;
                    card.Position.SetTileSlot(set.GetFirstCard().Position.GetTileSlot() + offset);
                    gameBoard.PlayCardOnBoard(card, set.GetFirstCard().Position.GetTileSlot() + offset, false);
                }
            }

        }

        foreach (Card cardToUpdate in cardsToUpdateFromBoard)
        {
            gameBoard.MoveCardFromGameBoardToGameBoard(cardToUpdate);
        }
        // play the cards that were added to the sets
        foreach (Card cardToUpdate in cardsToUpdateFromPlayer)
        {
            gameBoard.MoveCardFromPlayerHandToGameBoard(cardToUpdate, false);
        }
        // remove the cards that were added to the sets
        foreach (Card card in cardsToRemove)
        {
            print("remove: " + card.ToString());
            myPlayer.RemoveCardFromList(card);
        }

    }

    // Sort the cards by group
    /// <summary>
    /// Sorts the player's hand by grouping cards with the same number together and different colors (duplicates beside each other).
    /// </summary>
    public void SortByGroup()
    {
        myPlayer.GetPlayerHand().Sort((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Color.CompareTo(card2.Color);
            else
                return card1.Number.CompareTo(card2.Number);
        });
    }

    // Sort the cards by run 
    /// <summary>
    /// Sorts the player's hand by grouping cards with the same color together and different numbers (duplicates beside each other).
    /// </summary>
    public void SortByRun()
    {
        myPlayer.GetPlayerHand().Sort((card1, card2) =>
        {
            if (card1.Color == card2.Color)
                return card1.Number.CompareTo(card2.Number);
            else
                return card1.Color.CompareTo(card2.Color);
        });
    }
}