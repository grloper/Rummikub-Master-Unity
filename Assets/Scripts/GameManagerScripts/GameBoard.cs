
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    // List of cards in the HumanHand's hand
    private List<Card> humanHand = new List<Card>();
    // List of cards in the computer's hand
    private List<Card> computerHand = new List<Card>();
    // List of valid sets on the board
    private List<List<CardsSet>> gameBoardValidSets = new List<List<CardsSet>>();
    // Single Instance! Deck of cards
    private RummikubDeck rummikubDeck = new RummikubDeck();
    // Stack of moves that the player made so we can undo them
    private Stack<Card> movesStack = new Stack<Card>();
    // refeence to the HumanGrid
    [SerializeField] GameObject HumanGrid;
    // refeence to the BoardGrid
    [SerializeField] GameObject BoardGrid;
    // Reference to Human so we can draw cards
    [SerializeField] Human human;
    // Reference to Computer so we can draw cards
    [SerializeField] Computer computer;
    // Reference to GameManager so we can change turns
    [SerializeField] GameManager gameManager;

    // Start is called before the first frame update
    private void Start()
    {
        //game board valid sets need to have at max 0-7 sets which is the rows on board and every set can have many CardsSet which will held by location on board
        gameBoardValidSets = new List<List<CardsSet>>();
        for (int i = 0; i < Constants.MaxBoardRows; i++)
        {
            gameBoardValidSets.Add(new List<CardsSet>());
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
        if (!card.CameFromHumanHand)
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
                draggableItem.parentAfterDrag.SetParent(card.ParentBeforeDrag);
                card.transform.SetParent(draggableItem.parentAfterDrag);
                card.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // working
            Debug.Log("<color=yellow>from board to human undo</color>");
            MoveCardFromGameBoardToHumanHand(card);
            GameObject tileSlot = HumanGrid.transform.GetChild(human.GetEmptySlotIndex()).gameObject;
            card.transform.SetParent(tileSlot.transform);
            card.transform.localPosition = Vector3.zero;
        }
    }
    // add card to the stack of moves   
    public void AddCardToMovesStack(Card card) => movesStack.Push(card);
    public Stack<Card> GetMovesStack() => movesStack;

    // Move Card from GameBoard to GameBoard
    public void MoveCardFromGameBoardToHumanHand(Card card)
    {
        foreach (CardsSet cardsSet in gameBoardValidSets[card.Position.Row])
        {
            if (cardsSet.IsContainsCard(card))
            {
                cardsSet.RemoveCard(card);
                if (cardsSet.set.Count == Constants.EmptyCardsSet)
                    gameBoardValidSets[card.Position.Row].Remove(cardsSet);
                humanHand.Add(card);
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
    public void MoveCardFromGameBoardToGameBoard(Card card)
    {
        int i=-1;
        foreach (CardsSet cardsSet in gameBoardValidSets[card.OldPosition.Row])
        {
            if (cardsSet.IsContainsCard(card))
            {
               //save the index of the card in the set
               i = cardsSet.RemoveCard(card);
                // if the card were removed from the set in the middle uncombine into two sets
                if (i > 0 && i < cardsSet.set.Count)
                {
                    CardsSet set = cardsSet.UnCombine(i);
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
    public void MoveCardFromHumanHandToGameBoard(Card card)
    {
        PutInSet(card);
        humanHand.Remove(card);
    }
    //complexity O(n^2) when n is the number of sets in the row 
    public void PutInSet(Card card)
    {
        //add card in the row postion in a new CardsSet
        if (gameBoardValidSets[card.Position.Row].Count == Constants.ZeroCardsSet)
        {
            //add the new CardSet to the set
            gameBoardValidSets[card.Position.Row].Add(new CardsSet(card));
        }
        else
        {
            // iterate over the CardsSet in the row and check if the card can be added to the one of them at the beginning or at the end
            foreach (CardsSet cardSet in gameBoardValidSets[card.Position.Row])
            {

                if (cardSet.CanAddCardBeggining(card)) // if the card can be added to the beginning of the set, add it
                    cardSet.AddCardBeggining(card);
                else if(cardSet.CanAddCardEnd(card)) // if the card can be added to the end of the set, add it
                    cardSet.AddCardEnd(card);
                Combine(card.Position); // combine the sets if they are consecutive
                return;
            }
            // if the card can't be added to any of the sets in the row add it to a new set
             gameBoardValidSets[card.Position.Row].Add(new CardsSet(card));
        }
    }

    public Card GetRandomCardFromComputer()
    {
        if (computerHand.Count==Constants.EmptyStack)
        {
            throw new EmptyDeckException();
        }
        int index = UnityEngine.Random.Range(0, computerHand.Count);
        Card card = computerHand[index];
        computerHand.RemoveAt(index);
        return card;
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

    // Move Card from ComputerHand to GameBoard
    public void MoveCardFromComputerHandToGameBoard(Card card)
    {
       computerHand.Remove(card);
       PutInSet(card);
    }

    // Add Card to HumanHand
    public void AddCardToHumanHand(Card card) => humanHand.Add(card);

    // Return instance of human hand
    public List<Card> GetHumanHand() => humanHand;

    // Add Card to ComputerHand
    public void AddCardToComputerHand(Card card) => computerHand.Add(card);

    // Return instance of computer hand

    public List<Card> GetComputerHand()
    {
        // clone computerHand   
        List<Card> clone = new List<Card>();
        foreach (Card card in computerHand)
        {
            clone.Add(card);
        }
        return clone;
    }

    // Explain the game rules
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
    public RummikubDeck GetRummikubDeckInstance() => rummikubDeck;


    public bool IsBoardValid()
    {
        return true;
        bool humanCheck = human.GetInitialMove() && gameManager.GetTurn() == Constants.HumanTurn; // if the human has made the initial move
        bool computerCheck = computer.GetInitialMove() && gameManager.GetTurn() == Constants.AITurn; // if the computer has made the initial move 
        if (GetMovesStackSum() >= Constants.MinFirstSet || humanCheck || computerCheck)
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
        if (!human.GetInitialMove() && gameManager.GetTurn() == Constants.HumanTurn)
            human.SetInitialMove(true);
        else if (!computer.GetInitialMove() && gameManager.GetTurn() == Constants.AITurn)
            computer.SetInitialMove(true);
    }


    // Check if a set of cards is valid
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

    public void PrintComputerHand()
    {
        Debug.Log("<color=green>Computer Hand:</color>");

        // Use a counter to append a unique identifier to each card message
        int counter = 0;
        foreach (Card card in computerHand)
        {
            // Append the counter to the card message
            Debug.Log($"Card {counter}: {card}");
            counter++;
        }

    }

    public void PrintHumanHand()
    {
        Debug.Log("<color=green>Human Hand:</color>");

        // Use a counter to append a unique identifier to each card message
        int counter = 0;
        foreach (Card card in humanHand)
        {
            // Append the counter to the card message
            Debug.Log($"Card {counter}: {card}");
            counter++;
        }
    }

}
