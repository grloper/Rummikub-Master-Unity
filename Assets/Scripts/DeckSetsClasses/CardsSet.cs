
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class CardsSet : ICardSet
{
    // a certain set of cards on board
    public DoublyLinkedList<Card> set;
    public bool isRun;
    public bool isGroupOfColors;
    // 3-13 cards in a set if it represents a run 
    // 3-4 cards in a set if it represents a group of colors
    // Default Constructor
    public CardsSet()
    {
        set = new DoublyLinkedList<Card>();
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
        set = new DoublyLinkedList<Card>();
        Node<Card> currentNode = cardsSet.set.GetFirstNode();
        while (currentNode != null)
        {
            set.AddLast(currentNode.Value);
            currentNode = currentNode.Next;
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
    public Card GetFirstCard() => set.GetFirstNode().Value;

    // O(1)
    public Card GetLastCard() => set.GetLastNode().Value;
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

    // check if the set contains a certain card
    // O(n)
    public bool IsContainsCard(Card card)
    {
        return set.Contains(card);
    }
    // Combine two sets of cards and return the new set of cards
    // O(n) when n is the number of cards in the second set

    // Combine two sets of cards and return the new set of cards, O(1)
    public CardsSet Combine(CardsSet set1, CardsSet set2)
    {
        //append set2 to set2 while both contain set.set which is DoublyLinkedList<Card>
        set1.set.Append(set2.set);
        return set1;
    }


    // Uncombine the set of cards and return the new set of cards,
    // the offset is the number of cards to remove from the set because they are at another list
    // O(n) when n is the number of cards to remove
    public CardsSet UnCombine(int offset)
    {
        Debug.Log("uncombine: "+ offset );
        CardsSet newSet = new CardsSet();
        for (int i = 0; i < offset; i++)
        {
            newSet.set.AddLast(set.GetFirstNode().Value);
            set.RemoveFirst();
        }
        return newSet;
    }

    // Remove a card from the set and return its index in the set
    // O(n) when n is the number of cards in the set

public int RemoveCard(Card card)
{
    Node<Card> current = set.GetFirstNode();
    int i = 0;
    while (current != null)
    {
        if (current.Value.Equals(card))
        {
            set.Remove(current);
            return i;
        }
        current = current.Next;
        i++;
    }
    return -1;
}

    // Check if a run is valid
    // O(n) when n is the number of cards in the set

    public bool IsRun()
    {
        if (set == null || set.Count < Constants.MinInRun || set.Count > Constants.MaxInRun)
        {
            return isRun = false; // A run must have at least 3 cards but no more than 13
        }

        Node<Card> node = GetFirstNodeOfNotJoker();

        CardColor SetColor = node.Value.Color;
        int CurrentNum = node.Value.Number;
        int i = 0;
        for (Node<Card> current = node; current != null; current = current.Next, i++)
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
    public Node<Card> GetFirstNodeOfNotJoker()
    {
        Node<Card> node = set.GetFirstNode();
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
        Node<Card> current = set.GetFirstNode();
        while (current != null)
        {
            if (current.Value.Color == c)
            {
                return true;
            }
            current = current.Next;
        }
        return false;
    }
    public bool IsGroupOfColors()
    {
        if (set == null || (set.Count != Constants.MinInGroup && set.Count != Constants.MaxInGroup))
        {
            return isGroupOfColors = false; // A group must have either 3 or 4 cards
        }

        Node<Card> node = GetFirstNodeOfNotJoker();
        int CurrentNum = node.Value.Number;
        // Use a HashSet to track distinct colors
        HashSet<int> distinctColors = new HashSet<int>();

        for (Node<Card> current = node; current != null; current = current.Next)
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
        Node<Card> current = set.GetFirstNode();
        string setStr = "";
        while (current != null)
        {
            setStr += current.Value.ToString() + ", ";
            current = current.Next;
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
       AddCardToEnd(card);
        bool check = this.IsRun();
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

    // O(1)
    // return the index where the card should be if can be added. otherside return -1
    public int CanAddCardMiddleRun(Card card)
    {
        // Check if the set is long enough and the card has the same color O(1)
        if (set.Count < Constants.MinSetLengthForMiddleRun || card.Color != set.GetFirstNode().Value.Color)
            return -1;
        // Check if the card can be added in the middle O(1)
        if(card.Number<= set.GetFirstNode().Value.Number + Constants.MiddleRunOffset
         || card.Number >= set.GetLastNode().Value.Number- Constants.MiddleRunOffset)
        {
            // if the card can be added in the middle, return the index of the card
            return card.Number - set.GetFirstNode().Value.Number; //because the set is a run
        }
        return -1;
    }

    // End of CardsSet.cs
}