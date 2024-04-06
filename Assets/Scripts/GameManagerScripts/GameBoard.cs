using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    private RummikubDeck rummikubDeck = new RummikubDeck();
    [SerializeField] private UImanager uiManager;
    private List<CardsSet>[] gameBoardValidSets = new List<CardsSet>[Constants.MaxBoardRows];
    private Stack<Card> movesStack = new Stack<Card>();
    private GameController gameController;


    // Return instance of rummikub deck
    public RummikubDeck GetRummikubDeckInstance() => rummikubDeck;

    // Start is called before the first frame update
    void Start()
    {
        //get game controller instance
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
        // this class is actually the board grid so we send "transform" 
        uiManager.InitBoardTileSlots(transform);
        //game board valid sets need to have at max 0-7 sets which is the rows on board and every set can have many CardsSet which will held by location on board
        gameBoardValidSets = new List<CardsSet>[Constants.MaxBoardRows];
        for (int i = 0; i < Constants.MaxBoardRows; i++)
        {
            gameBoardValidSets[i]=(new List<CardsSet>());
        }
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
                MoveCardFromGameBoardToGameBoard(card);
                //card.ParentBeforeDrag is the tile slot location on board when pushed first to the stack
                draggableItem.parentAfterDrag = card.ParentBeforeDrag;
                card.transform.parent=draggableItem.parentAfterDrag;
                card.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // working
            Debug.Log("<color=yellow>from board to player undo</color>");
            MoveCardFromGameBoardToPlayerHand(card);
            GameObject tileSlot = gameController.GetCurrentPlayer().GetPlayerGrid().transform.GetChild(gameController.GetCurrentPlayer().GetEmptySlotIndex()).gameObject;
            card.transform.SetParent(tileSlot.transform);
            card.transform.localPosition = Vector3.zero;
        }
    }

    // add card to the stack of moves   
    public void AddCardToMovesStack(Card card) => movesStack.Push(card);

    public Stack<Card> GetMovesStack() => movesStack;
    // Move Card from GameBoard to GameBoard
    public void MoveCardFromGameBoardToPlayerHand(Card card)
    {
        foreach (CardsSet cardsSet in gameBoardValidSets[card.Position.Row])
        {
            if (cardsSet.IsContainsCard(card))
            {
                cardsSet.RemoveCard(card);
                if (cardsSet.set.Count == Constants.EmptyCardsSet)
                    gameBoardValidSets[card.Position.Row].Remove(cardsSet);
                gameController.GetCurrentPlayer().AddCardToList(card);
                return;
            }
        }
    }
    // Print all items in gameBoardValidSets
    public void PrintGameBoardValidSets()
    {
        Debug.Log("<color=red>Print Sets:</color>");
        int count = Constants.EmptyCardsSet;
        foreach (List<CardsSet> set in gameBoardValidSets)
        {
            // Print the set number
            Debug.Log("<color=white>Set " + count + ":</color>");
            foreach (CardsSet cardsSet in set)
            {
                // Print The CardsSet
                Debug.Log("<color=orange>" + cardsSet.ToString() + "</color>");
            }
            count++;
        }
    }

    // Handle the movement of a card from the game board to the game board
    public async void MoveCardFromGameBoardToGameBoard(Card card)
    {
        int i = -1;
        foreach (CardsSet cardsSet in gameBoardValidSets[card.OldPosition.Row])
        {
            if (cardsSet.IsContainsCard(card))
            {
                //save the index of the card in the set
                i = cardsSet.RemoveCard(card);
                // if the card were removed from the set in the middle uncombine into two sets
                if (i > 0 && i < cardsSet.set.Count)
                {
                    CardsSet set = null;
                    Task.Run(() =>
                    {
                        set = cardsSet.UnCombine(i);
                    }).Wait(); // Wait for UnCombine to complete
                    gameBoardValidSets[card.OldPosition.Row].Add(set);
                }
                if (cardsSet.set.Count == Constants.EmptyCardsSet)
                {
                    gameBoardValidSets[card.OldPosition.Row].Remove(cardsSet);
                }
                PutInSet(card);
                return;
            }
        }
    }
    public void MoveCardFromPlayerHandToGameBoard(Card card)
    {
        PutInSet(card);
        gameController.GetCurrentPlayer().RemoveCardFromList(card);
    }
    //complexity O(n^2) when n is the number of sets in the row 
    public void PutInSet(Card card)
    {
        //add card in the row postion in a new CardsSet
        //print in blue the card postion row
        if (gameBoardValidSets[card.Position.Row].Count == Constants.ZeroCardsSet)
        {
            print("<color=blue>card postion row: " + card.Position.Row + "</color>");
            //add the new CardSet to the set
            gameBoardValidSets[card.Position.Row].Add(new CardsSet(card));
        }
        else
        {
            // iterate over the CardsSet in the row and check if the card can be added to the one of them at the beginning or at the end
            foreach (CardsSet cardSet in gameBoardValidSets[card.Position.Row])
            {

                if (cardSet.CanAddCardBeggining(card)) // if the card can be added to the beginning of the set, add it
                {
                    cardSet.AddCardToBeggining(card);
                    Task.Run(() =>
                    {
                        Combine(card.Position); // combine the sets if they are consecutive
                    }).Wait(); // Wait for Combine to complete
                    return;
                }
                else if (cardSet.CanAddCardEnd(card)) // if the card can be added to the end of the set, add it
                { 
                   cardSet.AddCardToEnd(card);
                    Task.Run(() =>
                    {
                        Combine(card.Position); // combine the sets if they are consecutive
                    }).Wait(); // Wait for Combine to complete
                    return;
                }
            }
            // if the card can't be added to any of the sets in the row add it to a new set
            gameBoardValidSets[card.Position.Row].Add(new CardsSet(card));
        }
    }


    // function that combine two sets if they are consecutive by postion of column
    public void Combine(CardPosition cardPosition)
    {
        foreach (CardsSet cardsSet in gameBoardValidSets[cardPosition.Row])
        {
            foreach (CardsSet otherSet in gameBoardValidSets[cardPosition.Row])
            {
                if (cardsSet != otherSet)
                {
                    if (AreConsecutive(cardsSet.GetLastCard().Position.Column, otherSet.GetFirstCard().Position.Column))
                    {
                        cardsSet.Combine(cardsSet, otherSet);
                        gameBoardValidSets[cardPosition.Row].Remove(otherSet);
                        return;
                    }
                    else if (AreConsecutive(cardsSet.GetFirstCard().Position.Column, otherSet.GetLastCard().Position.Column))
                    {
                        otherSet.Combine(otherSet, cardsSet);
                        gameBoardValidSets[cardPosition.Row].Remove(cardsSet);
                        return;
                    }
                }
            }
        }
    }
    // Check if two numbers are consecutive
    public bool AreConsecutive(int num1, int num2) => Math.Abs(num1 - num2) == 1;
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
        bool humanCheck = gameController.GetCurrentPlayer().GetInitialMove(); // if the human has made the initial move
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
        return false; // cards sum is less than 30 
    }
    public void UpdateIntialMove()
    {
        // Update the initial move for the player - means he dropped sets with more than 30 points
        if (!gameController.GetCurrentPlayer().GetInitialMove() )
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
                if (emptySlotsCount[row] == amount +1&& (i - amount) % Constants.MaxPlayerColumns == 0)
                {
                    return i - amount;
                }
                else if (emptySlotsCount[row] == amount+1)
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
            gameController.GetCurrentPlayer().RemoveCardFromList(card);
            card.Position = new CardPosition(tileslot);
            uiManager.MoveCardToBoard(card,tileslot);
            tileslot++;
            AddCardToMovesStack(card);
            MoveCardFromGameBoardToPlayerHand(card);
        }
    }
}