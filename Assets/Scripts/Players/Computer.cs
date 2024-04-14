using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private List<Card> computerHand;
    public float computerMoveDelay = 0.9f;
    // Player reference
    private Player myPlayer;

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
            if (list[i].Equals(list[i - 1]))
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
            if (list[i].Equals(list[i - 1]))
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


    public bool MaximizeValidDrops()
    {
        bool dropped = false;
        List<CardsSet> setsRun = ExtractMaxValidRunSets(this.computerHand, Constants.MinInRun, Constants.MaxInRun);
        if (setsRun.Count > 0)
        {
            dropped = true;
            foreach (CardsSet set in setsRun)
            {
                gameBoard.PlayCardSetOnBoard(set);
            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(this.computerHand, Constants.MinInGroup, Constants.MaxInGroup);
        if (setsGroup.Count > 0)
        {
            dropped = true;
            foreach (CardsSet set in setsGroup)
            {
                gameBoard.PlayCardSetOnBoard(set);
            }
        }
        return dropped;

    }
    public void Initialize(Player player)
    {
        print("init");
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
        bool dropped = MaximizeValidDrops();
        //  MaximizePartialDrops();

        bool added = AssignFreeCardsToExistsSets();
        if (dropped || added)
        {
            uiManager.ConfirmMove();
        }
        else
        {
            uiManager.DrawACardFromDeck();
        }


    }


    private bool AssignFreeCardsToExistsSets()
    {
        bool added = false;

        foreach (Card card in this.computerHand)
        {
            foreach (CardsSet set in gameBoard.GetGameBoardValidSetsTable().Values)
            {
                if (set.CanAddCard(card))
                {
                    int tileslot = -1;
                    int leftKey = -1;
                    int rightKey = -1;
                    SetPosition setPos = new SetPosition(-1);
                    int newLeftkey = -1;
                    int newRightkey = -1;
                    if (set.CanAddCardEndRun(card) || set.CanAddCardEndGroup(card))
                    {
                        // If the card can be added to the end of the set, calculate the tileslot accordingly
                        tileslot = gameBoard.GetEmptySlotIndexFromGameBoard(set.GetDeckLength() + 1);
                        added = true;
                        foreach (Card cardInSet in set.set)
                        {
                            cardInSet.Position = new CardPosition(tileslot); // save the first location
                            uiManager.MoveCardToBoard(cardInSet, tileslot); // start from the first card till the end
                            tileslot++;
                        }
                        card.Position = new CardPosition(tileslot);// save the last location
                        newLeftkey = gameBoard.GetKeyFromPosition(set.GetFirstCard().Position);
                        newRightkey = gameBoard.GetKeyFromPosition(set.GetLastCard().Position);
                        newRightkey++;
                        //update the new keys
                        // we assume the board is always valid ( more than 2 cards in the set)
                        gameBoard.GetCardsInSetsTable()[newLeftkey] = setPos;
                        gameBoard.GetCardsInSetsTable()[newRightkey] = setPos;
                        gameBoard.GetCardsInSetsTable().Remove(leftKey);
                        gameBoard.GetCardsInSetsTable().Remove(rightKey);
                        gameBoard.PlayCardOnBoard(card, tileslot); // add last

                    }
                    else if (set.CanAddCardBegginingRun(card) || set.CanAddCardBegginingGroup(card))
                    {
                        leftKey = gameBoard.GetKeyFromPosition(set.GetFirstCard().Position);
                        rightKey = gameBoard.GetKeyFromPosition(set.GetLastCard().Position);
                        setPos = gameBoard.GetCardsInSetsTable()[leftKey];

                        //card pos = tileslot, set[0]. pos = tileslot++
                        // If the card can be added to the end of the set, calculate the tileslot accordingly
                        tileslot = gameBoard.GetEmptySlotIndexFromGameBoard(set.GetDeckLength());
                        card.Position = new CardPosition(tileslot); // save the first location
                        added = true;
                        foreach (Card cardInSet in set.set)
                        {
                            tileslot++;//start from the second card till the end
                            cardInSet.Position = new CardPosition(tileslot); // save the first location
                            uiManager.MoveCardToBoard(cardInSet, tileslot);
                        }
                        newLeftkey = gameBoard.GetKeyFromPosition(set.GetFirstCard().Position);
                        newLeftkey--;
                        newRightkey = gameBoard.GetKeyFromPosition(set.GetLastCard().Position);
                        //update the new keys
                        // we assume the board is always valid ( more than 2 cards in the set)
                        gameBoard.GetCardsInSetsTable()[newLeftkey] = setPos;
                        gameBoard.GetCardsInSetsTable()[newRightkey] = setPos;
                        gameBoard.GetCardsInSetsTable().Remove(leftKey);
                        gameBoard.GetCardsInSetsTable().Remove(rightKey);
                        gameBoard.PlayCardOnBoard(card, tileslot); // add first  
                    }

                }
            }
        }
        return added;
    }

    public void PrintCards()
    {
        print("------------------------------------------Set:------------------------------------------");
        foreach (Card card in computerHand)
        {
            print(card.ToString());
        }
    }


    public void SortByGroup()
    {
        //print("Group");
        // Sort the cards by group
        computerHand.Sort((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Color.CompareTo(card2.Color); // group by same number and differnt colors (duplicates beside eachother)
            else
                return card1.Number.CompareTo(card2.Number);
        });
        //PrintCards();

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