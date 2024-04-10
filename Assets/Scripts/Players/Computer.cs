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
             && currentSet.set.Count <=maxRangeInclusive)
            {
                currentSet.AddCardToEnd(list[i]);
            }
            else
            {
                if (currentSet.set.Count >= minRangeInclusive&& currentSet.set.Count<=maxRangeInclusive)
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
    private int GetTotalCards(List<CardsSet> list)
    {
        int totalCards = 0;
        foreach (CardsSet set in list)
        {
            totalCards += set.set.Count();
        }
        return totalCards;

    }

   public  bool MaximizeValidDrops()
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
        MaximizePartitialDrops();
        if (dropped == true)
        {
            uiManager.ConfirmMove();
        }
        else
        {
            uiManager.DrawACardFromDeck();
        }
       
    }

    private bool MaximizePartitialDrops()
    {
        bool dropped = false;
        List<CardsSet> setsRun = ExtractMaxValidRunSets(this.computerHand, Constants.MaxPartialSet, Constants.MaxPartialSet);
        List<Card> freeCardsFromBoard = new List<Card>();
        if (setsRun.Count > 0)
        {
            foreach (CardsSet set in setsRun)
            {
                // can we add to the begining or to the end witht he extracted free cards:
                freeCardsFromBoard = ExtractFreeCardsFromBoard();

            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(this.computerHand, Constants.MaxPartialSet, Constants.MaxPartialSet);
        if (setsGroup.Count > 0)
        {
            foreach (CardsSet set in setsGroup)
            {
                freeCardsFromBoard = ExtractFreeCardsFromBoard();
            }
        }
        return dropped;
    }

    private List<Card> ExtractFreeCardsFromBoard()
    {
        return null;
        
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
      //  print("Run");
        computerHand.Sort((card1, card2) =>
        {
            if (card1.Color == card2.Color)
                return card1.Number.CompareTo(card2.Number);
            else
                return card1.Color.CompareTo(card2.Color);
        });
       // PrintCards();

    }

     

}