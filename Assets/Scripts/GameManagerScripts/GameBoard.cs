﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class GameBoard : MonoBehaviour
{
    private RummikubDeck rummikubDeck = new RummikubDeck();
    [SerializeField] private UImanager uiManager;
    //private List<CardsSet>[] gameBoardValidSets = new List<CardsSet>[Constants.MaxBoardRows];

    // <int = CardPosition.Row *1 - represent the start of an existing set
    private Dictionary<int, SetPosition> cardsInSets;
    private Dictionary<SetPosition, CardsSet> gameBoardValidSets;
    private Stack<Card> movesStack = new Stack<Card>();
    private GameController gameController;
    private static int SetCount;


    // Return instance of rummikub deck
    public RummikubDeck GetRummikubDeckInstance() => rummikubDeck;
    public Dictionary<int, SetPosition> GetCardsInSetsTable() => cardsInSets;

    public Dictionary<SetPosition, CardsSet> GetSets() => gameBoardValidSets;

    // Start is called before the first frame update
    void Start()
    {
        SetCount = 0;
        //get game controller instance
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
        // this class is actually the board grid so we send "transform" 
        uiManager.InitBoardTileSlots(transform);
        //game board valid sets need to have at max 0-7 sets which is the rows on board and every set can have many CardsSet which will held by location on board
        cardsInSets = new Dictionary<int, SetPosition>();
        gameBoardValidSets = new Dictionary<SetPosition, CardsSet>();
        ExplainGameRules();

    }
    public void UndoMoves()
    {
        while (movesStack.Count > Constants.EmptyStack)
        {
            Card card = movesStack.Pop();
            UndoMoveForCard(card);
        }
        PrintGameBoardValidSets();
    }
    public bool IsExistForStack(Card card) => movesStack.Contains(card);
    private void UndoMoveForCard(Card card)
    {
        // Undo logic for a single card
        if (!card.CameFromPlayerHand)
        {
            // Move the card back to its original position
            DraggableItem draggableItem = card.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                Debug.Log("<color=yellow>from board to board undo</color>");
                // change the position of the card to match the logic of the PutInSet() called in the MoveCardFromGameBoardToGameBoard
                CardPosition tempPos = card.Position;
                card.Position = card.OldPosition;
                card.OldPosition = tempPos;
                //  MoveCardFromGameBoardToGameBoard(card);
                //card.ParentBeforeDrag is the tile slot location on board when pushed first to the stack
                draggableItem.parentAfterDrag = card.ParentBeforeDrag;
                card.transform.parent = draggableItem.parentAfterDrag;
                card.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // working
            Debug.Log("<color=yellow>from board to player undo</color>");
            //  MoveCardFromGameBoardToPlayerHand(card);
            GameObject tileSlot = gameController.GetCurrentPlayer().GetPlayerGrid().transform.GetChild(gameController.GetCurrentPlayer().GetEmptySlotIndex()).gameObject;
            card.transform.SetParent(tileSlot.transform);
            card.transform.localPosition = Vector3.zero;
        }
    }

    // add card to the stack of moves   
    public void AddCardToMovesStack(Card card) => movesStack.Push(card);

    public Stack<Card> GetMovesStack() => movesStack;
    // Move Card from GameBoard to GameBoard
    //public void MoveCardFromGameBoardToPlayerHand(Card card)
    // {
    //   foreach (CardsSet cardsSet in gameBoardValidSets[card.Position.Row])
    //   {
    //     if (cardsSet.IsContainsCard(card))
    //    {
    //      cardsSet.RemoveCard(card);
    //       if (cardsSet.set.Count == Constants.EmptyCardsSet)
    //                    gameBoardValidSets[card.Position.Row].Remove(cardsSet);
    //    gameController.GetCurrentPlayer().AddCardToList(card);
    //   return;
    //       }
    ////     }
    //  }
    // Print all items in gameBoardValidSets
    public void PrintGameBoardValidSets()
    {
        Debug.Log("<color=red>Print Sets:</color>");
        int count = Constants.EmptyCardsSet;
        foreach (CardsSet set in gameBoardValidSets.Values)
        {
            // Print The CardsSet
            Debug.Log("<color=orange>" + set.ToString() + "</color>");
            count++;
        }
    }


    // Handle the movement of a card from the game board to the game board
    public void MoveCardFromGameBoardToGameBoard(Card card)
    {
        SetPosition oldSetPos = new SetPosition(-1);
        CardsSet oldSet = new CardsSet();
        int row = card.OldPosition.Row;
        int col = card.OldPosition.Column;

        int key = GetKeyFromPosition(card.OldPosition);
        if (!cardsInSets.ContainsKey(key))
        {
            bool found = false;
            // Move left
            for (int i = col; i >= 0 && !found; i--)
            {
                key = row * 100 + i;
                if (cardsInSets.ContainsKey(key))
                {
                    oldSetPos = cardsInSets[key];
                    oldSet = gameBoardValidSets[oldSetPos];
                    found = true;
                }
            }
        }
        else
        {
            oldSetPos = cardsInSets[key];
            oldSet = gameBoardValidSets[oldSetPos];
        }

        int cardIndex = oldSet.RemoveCard(card);
        if (oldSet.set.Count == 0)
        {
            cardsInSets.Remove(key);
            gameBoardValidSets.Remove(oldSetPos);
        }
        // If the card was removed from the middle of the set, split the set into two
        if (oldSet.set.Count > 1)
        {
            if (cardIndex > 0 && cardIndex < oldSet.set.Count - 1)
            {
                // new set = the set from the left 
                // set from the right is the old set with the card removed, oldSetPos
                CardsSet newSet = oldSet.UnCombine(cardIndex);
                SetPosition newSetPos = new SetPosition(SetCount++);
                gameBoardValidSets[newSetPos] = newSet;
                //upate the first card and last card to point to that newSetPos
                cardsInSets[GetKeyFromPosition(newSet.GetFirstCard().Position)] = newSetPos;
                cardsInSets[GetKeyFromPosition(newSet.GetLastCard().Position)] = newSetPos;
                // update the last card in the old set to point to the oldSetPos
                cardsInSets[GetKeyFromPosition(oldSet.GetLastCard().Position)] = oldSetPos;
                cardsInSets[GetKeyFromPosition(oldSet.GetFirstCard().Position)] = oldSetPos;

                // Update the cardsInSets dictionary to reflect the split
            }
            else
            {
                // If the card was removed from the beginning or the end of the set,
                // for the begining -> 1,2,3,4
                //1 and 4 point to that set
                // so we want 2 to point to that set, and remove 1 from the dictionary
                // for the end -> 1,2,3,4
                //1 and 4 point to that set
                // so we want 3 to point to that set, and remove 4 from the dictionary
                if (cardIndex == 0 && oldSet.set.Count > 0)
                {
                    cardsInSets.Remove(GetKeyFromPosition(oldSet.GetFirstCard().Position));
                }
                else if (cardIndex == oldSet.set.Count && oldSet.set.Count > 0)
                {
                    cardsInSets.Remove(GetKeyFromPosition(oldSet.GetLastCard().Position));
                }
            }
        }
        // If the old set is now empty, remove it


        // Add the card to its new position
        PutInSet(card);
    }

    public void MoveCardFromPlayerHandToGameBoard(Card card)
    {
        gameController.GetCurrentPlayer().RemoveCardFromList(card);
        PutInSet(card);
    }

    // hash function 
    private int GetKeyFromPosition(CardPosition cardPosition)
    {
        return (cardPosition.Row * 100) + cardPosition.Column;
    }


    public void PutInSet(Card card)
    {
        int key = GetKeyFromPosition(card.Position);
        int rightKey = key + 1;
        int leftKey = key - 1;

        if (cardsInSets.ContainsKey(rightKey) && cardsInSets.ContainsKey(leftKey))
        {
            // Combine two sets gameBoardValidSets[cardsInSets[leftKey]].set and gameBoardValidSets[cardsInSets[rightKey].set]
            SetPosition leftSetPos = cardsInSets[leftKey];
            SetPosition rightSetPos = cardsInSets[rightKey];
            CardsSet leftSet = gameBoardValidSets[leftSetPos];
            CardsSet rightSet = gameBoardValidSets[rightSetPos];
            cardsInSets[GetKeyFromPosition(rightSet.GetLastCard().Position)] = leftSetPos;
            leftSet.AddCardToEnd(card);
            leftSet.Combine(leftSet, rightSet);
            gameBoardValidSets.Remove(rightSetPos);
            if (leftSet.set.Count != 3)
            {
                cardsInSets.Remove(rightKey);
                cardsInSets.Remove(leftKey);
            }



        }
        else if (cardsInSets.ContainsKey(rightKey))
        {
            // Add card to the beginning of the set
            SetPosition setPos = cardsInSets[rightKey];
            gameBoardValidSets[setPos].AddCardToBeginning(card);
            if (gameBoardValidSets[setPos].set.Count != 2)
            {
                cardsInSets.Remove(rightKey);
            }
            //added worked 
            cardsInSets[key]= setPos;
        }
        else if (cardsInSets.ContainsKey(leftKey))
        {
            // Add card to the end of the set
            SetPosition setPos = cardsInSets[leftKey];
            gameBoardValidSets[setPos].AddCardToEnd(card);

            if (gameBoardValidSets[setPos].set.Count != 2)
            {
                cardsInSets.Remove(leftKey);
            }
            cardsInSets[key] = setPos;
        }
        else
        {
            // Create a new set
            SetPosition newSetPos = new SetPosition(SetCount++);
            gameBoardValidSets[newSetPos] = new CardsSet(card);

            cardsInSets[key] = newSetPos;
        }
    }
    public void ExplainGameRules()
    {
        print("The game is played with two sets of 52 cards and 2 jokers. Each " +
            "player has 14 cards in his hand. The goal of the game is to get rid of all the cards in your hand. " +
            "You can do this by creating sets of cards. There are two types of sets: a group and a run. A group is a set of " +
            "3 or 4 cards with the same number but different colors. A run is a set of 3 or more cards with the same color and" +
            " consecutive numbers. A joker can be used as any card. You can add cards to the sets on the board or create new sets." +
            " You can also move cards inside the boards as long as you are not breaking the rules and keeps all the sets valids.");
    }
    // Get the sum of the moves on the board 
    public int GetMovesStackSum()
    {
        int sum = Constants.EmptyStack;
        foreach (Card card in GetMovesStack())
        {
            sum += card.Number;
        }
        return sum;
    }

    // Return instance of rummikub deck

    public bool IsBoardValid()
    {
        return true;
        /*  bool humanCheck = true; gameController.GetCurrentPlayer().GetInitialMove(); // if the human has made the initial move
          if (GetMovesStackSum() >= Constants.MinFirstSet || humanCheck)
          {
              foreach (List<CardsSet> listCardsSet in gameBoardValidSets) // scan all the CardsSets in the board
                  foreach (CardsSet cardsSet in listCardsSet)
                      if (!IsSetValid(cardsSet)) { return false; }
              UpdateIntialMove(); // update the initial move if needed
              return true; // Board is valid
          }
          //print in red need to drop more than 30 points
          Debug.Log("<color=red>Need to drop more than 30 points</color>");
          return false; // cards sum is less than 30  */
    }
    public void UpdateIntialMove()
    {
        // Update the initial move for the player - means he dropped sets with more than 30 points
        if (!gameController.GetCurrentPlayer().GetInitialMove())
            gameController.GetCurrentPlayer().SetInitialMove(true);

    }


    // Check if a set of cards is valid (either run or either group)
    private bool IsSetValid(CardsSet cardsSet)
    {
        if (!cardsSet.IsRun() && !cardsSet.IsGroupOfColors())
        {
            Debug.Log($"Invalid set: {string.Join(", ", cardsSet)}");
            Debug.Log($"IsRun: {cardsSet.IsRun()}, IsGroupOfColors: {cardsSet.IsGroupOfColors()}");
            return false; // Set is invalid
        }
        return true; // Set is valid
    }

    // complexity O(n)

    public int GetEmptySlotIndexFromGameBoard(int amount)
    {
        GameObject BoardGrid = GameObject.FindGameObjectWithTag("BoardGrid");

        int rowCount = Constants.MaxBoardRows;
        int colCount = Constants.MaxBoardColumns;
        int[] emptySlotsCount = new int[rowCount]; // Array to store the count of consecutive empty slots on each row

        // Iterate through the board and count consecutive empty slots on each row
        for (int i = 0; i < BoardGrid.transform.childCount; i++)
        {
            GameObject currentSlot = BoardGrid.transform.GetChild(i).gameObject;
            int row = i / colCount;

            // Check if the slot is empty
            if (currentSlot.transform.childCount == Constants.EmptyTileSlot)
            {
                emptySlotsCount[row]++;
                // Check if we have found 'amount' consecutive empty slots on the same row
                if (emptySlotsCount[row] == amount + 1 && (i - amount) % Constants.MaxBoardColumns == 0)
                {
                    return i - amount;
                }
                else if (emptySlotsCount[row] == amount + 1)
                {
                    // Calculate the index of the first slot in the sequence
                    return i - amount + 1;
                }
            }
            else
            {
                // Reset the count if the slot is not empty
                emptySlotsCount[row] = 0;
            }
        }

        // Return -1 if no sequence of 'amount' consecutive empty slots on the same row is found
        return -1;
    }

    internal void PlayCardSetOnBoard(CardsSet cardsSet)
    {
        int tileslot = GetEmptySlotIndexFromGameBoard(cardsSet.set.Count);
        foreach (Card card in cardsSet.set)
        {
            card.Position = new CardPosition(tileslot);
            uiManager.MoveCardToBoard(card, tileslot);
            tileslot++;
            // in case of manual undo keep track of the logic for the computer even tho we allow only valid moves
            AddCardToMovesStack(card);
            // move and remove the card
            MoveCardFromPlayerHandToGameBoard(card);
        }
    }
    internal void PlayCardOnBoard(Card card, int tileslot)
    {
        // assume already check no nehibors to combine my love
        uiManager.MoveCardToBoard(card, tileslot);
        AddCardToMovesStack(card);
        MoveCardFromPlayerHandToGameBoard(card);

    }


}

