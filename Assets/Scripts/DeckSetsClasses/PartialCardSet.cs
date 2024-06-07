using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartialCardSet : ICardSet
{
    //this class is not for jokers only for regular Tiles
    // Contains only two cards!
    public List<Card> partialSet;


    // Constructor
    public PartialCardSet()
    {
        partialSet = new List<Card>();
    }


    // Constructor for adding a single card
    public PartialCardSet(Card card) : this()
    {
        AddCardToEnd(card);
    }


    // Method to check if a card can be added to the partial set
    public bool CanAddFirstCard()
    {
        return partialSet.Count == Constants.EmptyPartialSet; //0
    }
    public bool CanAddSecondCard()
    {
        return partialSet.Count == Constants.SingleCardInPartialSet; //1
    }
    public bool IsMaxPartialSet()
    {
        return partialSet.Count == Constants.MaxPartialSet; //2
    }
    public bool TryToAddCard(Card card)
    {
        partialSet.Add(card);
        if (!IsRun() && !IsGroupOfColors())
        {
            // If neither completes a run nor a group, remove the card and return false
            partialSet.Remove(card);
            return false;
        }
        return true;


    }



    public bool IsGroupOfColors()
    {
        if (partialSet == null || partialSet.Count != Constants.MaxPartialSet || // must have 2 cards
            IsSameColor(GetFirstCard(), GetLastCard().Color) || // must have different color
            !IsSameNumber(GetFirstCard(), GetLastCard())) // must have the same number
            return false;
        return true;
    }
    public bool IsSameNumber(Card c1, Card c2)
    {
        return c1.Number == c2.Number;
    }
    public bool IsRun()
    {
        if (partialSet == null || partialSet.Count != Constants.MaxPartialSet)
        {
            return false; // A run must have 2 cards
        }
        //sort by number 
        partialSet.Sort((x, y) => x.Number.CompareTo(y.Number));




        return IsConsicutive(GetFirstCard(), GetLastCard()) &&
                IsSameColor(GetFirstCard(), GetLastCard().Color);
    }
    public bool IsConsicutive(Card c1, Card c2)
    {
        return Mathf.Abs(c1.Number - c2.Number) == 1;
    }

    public bool IsSameColor(Card c1, CardColor color)
    {
        return c1.Color == color;
    }


    public bool IsContainsCard(Card card)
    {
        return partialSet.Contains(card);
    }

    public Card GetFirstCard()
    {
        return partialSet[0];
    }

    public Card GetLastCard()
    {
        return partialSet[1];
    }

    public void AddCardToBeginning(Card card)
    {
        partialSet.Insert(0, card);
    }

    public void AddCardToEnd(Card card)
    {
        partialSet.Add(card);
    }


    public override string ToString()
    {
        string setStr = "";
        foreach (Card c in partialSet)
        {
            setStr += c.ToString() + ", ";
        }
        return setStr;
    }


}