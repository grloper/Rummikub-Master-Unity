
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class CardsSet : ICardSet
{
    // a certain set of cards on board
    public LinkedList<Card> set;
    public bool isRun;
    public bool isGroupOfColors;
    // 3-13 cards in a set if it represents a run 
    // 3-4 cards in a set if it represents a group of colors
    // Default Constructor
    public CardsSet()
    {
        set = new LinkedList<Card>();
        isRun = false;
        isGroupOfColors = false;

    }
    // Constructor for a set of cards with a single card in it uses the default constructor
    public CardsSet(Card card) : this()
    {
        set.AddFirst(card);

    }
    public CardsSet(CardsSet cardsSet)
    {
        set = new LinkedList<Card>();
        foreach (Card card in cardsSet.set)
        {
            set.AddLast(card);
        }
        isRun = cardsSet.isRun;
        isGroupOfColors = cardsSet.isGroupOfColors;
    }

    public int GetDeckLength()
    {
        return set.Count;
    }
    // Utility methods
    //O(1)
    public Card GetFirstCard() => set.First.Value;

    // O(1)
    public Card GetLastCard() => set.Last.Value;
    // O(1)

    //add card beggining and end in two function 
    // O(1)
    public void AddCardToBeginning(Card card)
    {

        set.AddFirst(card);
    }

    // O(1)
    public void AddCardToEnd(Card card)
    {
        set.AddLast(card);
    }
    public bool CanAddCard(Card card)
    {
        return this.CanAddCardEnd(card) || this.CanAddCardBeggining(card);
    }

    // can add at the begginging of the set of List<Card> return bool true it check if the postion of the card colum + 1 is equal to the first card postion colum
    // O(1)
    public bool CanAddCardBeggining(Card card)
    {
        return GetFirstCard().Position.Column == card.Position.Column + 1;
    }
    // can add at the end of the set of List<Card> return bool true it check if the postion of the card colum - 1 is equal to the last card postion colum
    // O(1)
    public bool CanAddCardEnd(Card card)
    {
        return GetLastCard().Position.Column == card.Position.Column - 1;
    }



    // check if the set contains a certain card
    // O(n)
    public bool IsContainsCard(Card card)
    {
        return set.Contains(card);
    }
    // Combine two sets of cards and return the new set of cards
    // O(n) when n is the number of cards in the second set

    public CardsSet Combine(CardsSet set1, CardsSet set2)
    {
        //append set2 to set2 while both contain set.set which is LinkedList<Card>
        LinkedListNode<Card> node = set2.set.First;
        while (node != null)
        {
            set1.set.AddLast(node.Value);
            node = node.Next;
        }
        return set1;
    }


    // Uncombine the set of cards and return the new set of cards,
    // the offset is the number of cards to remove from the set because they are at another list
    // O(n) when n is the number of cards to remove
    public CardsSet UnCombine(int offset)
    {
        CardsSet newSet = new CardsSet();
        for (int i = 0; i < offset; i++)
        {
            newSet.set.AddLast(set.First.Value);
            set.RemoveFirst();
        }
        return newSet;
    }

    // Remove a card from the set and return its index in the set
    // O(n) when n is the number of cards in the set

    public int RemoveCard(Card card)
    {
        LinkedListNode<Card> node = set.Find(card);
        if (node == null)
        {
            return -1; // Card not found
        }

        int i = 0;
        for (LinkedListNode<Card> current = set.First; current != null; current = current.Next, i++)
        {
            if (current == node)
            {
                set.Remove(node);
                return i;
            }
        }

        return -1; // Should never reach here
    }

    // Check if a run is valid
    // O(n) when n is the number of cards in the set

    public bool IsRun()
    {
        if (set == null || set.Count < Constants.MinInRun || set.Count > Constants.MaxInRun)
        {
            return isRun = false; // A run must have at least 3 cards but no more than 13
        }

        LinkedListNode<Card> node = GetFirstNodeOfNotJoker();

        CardColor SetColor = node.Value.Color;
        int CurrentNum = node.Value.Number;
        int i = 0;
        for (LinkedListNode<Card> current = node; current != null; current = current.Next, i++)
        {
            if (CurrentNum == Constants.MaxRank + 1 || // the number is bigger than 13
               CurrentNum++ != current.Value.Number && !IsJoker(current.Value) || // the number is not in sequence (we also check for jokers = 0xf)
                !IsSameColor(current.Value, SetColor)) // the color is not the same
            {
                return isRun = false;
            }
        }
        return isRun = true;
    }

    // get the first node of a card that is not a joker
    // O(n) when n is the number of cards in the set
    public LinkedListNode<Card> GetFirstNodeOfNotJoker()
    {
        LinkedListNode<Card> node = set.First;
        for (int i = 0; i <= Constants.MaxJoker && node != null; i++, node = node.Next)
        {
            if (!IsJoker(node.Value))
            {
                return node;
            }
        }

        return null; // Default to null if all are jokers
    }

    // Check if a set contains a certain color
    // O(n) when n is the number of cards in the set
    public bool IsContainThisColor(CardColor c)
    {
        foreach (Card card in set)
        {
            if (card.Color == c)
            {
                return true;
            }
        }
        return false;
    }
    public bool IsGroupOfColors()
    {
        if (set == null || (set.Count != Constants.MinInGroup && set.Count != Constants.MaxInGroup))
        {
            return isGroupOfColors = false; // A group must have either 3 or 4 cards
        }

        LinkedListNode<Card> node = GetFirstNodeOfNotJoker();
        int CurrentNum = node.Value.Number;
        // Use a HashSet to track distinct colors
        HashSet<int> distinctColors = new HashSet<int>();

        for (LinkedListNode<Card> current = node; current != null; current = current.Next)
        {
            if (!IsJoker(current.Value) && (!distinctColors.Add((int)current.Value.Color) || CurrentNum != current.Value.Number))
            {
                return isGroupOfColors = false; // If a color is repeated, the group is invalid
            }
        }

        return isGroupOfColors = true;
    }
    // check if a card is the same color as the given color, if joker return true
    // O(1)
    public bool IsSameColor(Card c1, CardColor color)
    {
        return IsJoker(c1) || c1.Color == color;
    }
    // check if two cards are consecutive, if joker return true
    // O(1)
    public bool IsConsicutive(Card c1, Card c2)
    {
        return IsJoker(c1) || c1.Number == c2.Number + 1;
    }
    // check if a card is a joker and return true if it is
    // O(1)
    public bool IsJoker(Card card)
    {
        return card.Number == Constants.JokerRank; // the joker is a mask of 1111b
    }

    public override string ToString()
    {
        string setStr = "";
        foreach (Card c in set)
        {
            setStr += c.ToString() + ", ";
        }
        return setStr;
    }


    public bool CanAddCardBegginingRun(Card card)
    {
        AddCardToBeginning(card);
        bool check = this.IsRun();
        this.set.RemoveFirst();
        return check;

    }

    // check if a card can be added to the end of the set to create a run 
    // O(n) when n is the number of cards in the set
    public bool CanAddCardEndRun(Card card)
    {
        //add the card to the set O(1)
        AddCardToEnd(card);
        bool check = this.IsRun();
        //remove the card from the set O(1)
        this.set.RemoveLast();
        return check;
    }
    public bool CanAddCardBegginingGroup(Card card)
    {
        AddCardToBeginning(card);
        bool check = this.IsGroupOfColors();
        this.set.RemoveFirst();
        return check;

    }

    public bool CanAddCardEndGroup(Card card)
    {
        AddCardToEnd(card);
        bool check = this.IsGroupOfColors();
        this.set.RemoveLast();
        return check;
    }

    // return the index where the card should be if can be added. otherside return -1
    public int CanAddCardMiddleRun(Card card)
    {
        // Check if the set is long enough and the card has the same color O(1)
        if (set.Count < Constants.MinSetLengthForMiddleRun || card.Color != set.First.Value.Color)
            return -1;
        // Check if the card can be added to the middle of the set O(n) where n is the number of cards in the set - Constants.CardsToCheckForMiddleRun which max could be 13-5=8 O(8) = O(1)
        LinkedListNode<Card> node = set.Last;
        for (int i = set.Count - 1; i > set.Count - Constants.CardsToCheckForMiddleRun; i--)
        {
            if (card.Number == node.Value.Number + Constants.MiddleRunOffset)//return the insert index
                return i + Constants.MiddleRunOffset;
            node = node.Previous;
        }
        return -1;
    }



    /// <summary>
    /// Checks if there is space for a card in the specified position on the game board.
    /// </summary>
    /// <param name="isEnd">Indicates whether the card is being placed at the end of the set or at the beginning.</param>
    /// <param name="gameBoard">The game board object.</param>
    /// <returns>True if there is space for a card, false otherwise.</returns>
    public bool IsSpaceForCard(bool isEnd, GameBoard gameBoard)
    {
        if (isEnd)
        {
            return IsSpaceForCardAtEnd(gameBoard);
        }
        else
        {
            return IsSpaceForCardAtBeginning(gameBoard);
        }
    }

    private bool IsSpaceForCardAtEnd(GameBoard gameBoard)
    {
        GameObject secondTileSlot = null;
        int lastCardColumn = GetLastCard().Position.Column;
        if (lastCardColumn != Constants.MaxBoardColumns - 1 && lastCardColumn != Constants.MaxBoardColumns - 2)
        {
            int tileSlotIndex = GetLastCard().Position.GetTileSlot() + 2;
            if (tileSlotIndex < gameBoard.transform.childCount)
            {
                secondTileSlot = gameBoard.transform.GetChild(tileSlotIndex).gameObject;
            }
        }
        if (secondTileSlot != null && (secondTileSlot.transform.childCount == Constants.EmptyTileSlot || lastCardColumn == Constants.MaxBoardColumns - 2))
        {
            return true;
        }
        return false;
    }

    private bool IsSpaceForCardAtBeginning(GameBoard gameBoard)
    {
        GameObject secondTileSlot = null;
        int firstCardColumn = GetFirstCard().Position.Column;
        if (firstCardColumn != 0 && firstCardColumn != 1)
        {
            int tileSlotIndex = GetFirstCard().Position.GetTileSlot() - 2;
            if (tileSlotIndex >= 0)
            {
                secondTileSlot = gameBoard.transform.GetChild(tileSlotIndex).gameObject;
            }
        }
        if (secondTileSlot != null && (secondTileSlot.transform.childCount == Constants.EmptyTileSlot || firstCardColumn == 1))
        {
            return true;
        }
        return false;
    }


    // End of CardsSet.cs
}