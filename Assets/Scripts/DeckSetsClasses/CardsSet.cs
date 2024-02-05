using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class CardsSet 
{
    public List<Card> set;

    public CardsSet()
    {
        set = new List<Card>();
    }

    public CardsSet(Card card) : this()
    {
        set.Add(card);
    }

    // Utility methods
    public Card GetFirstCard() => set[0];

    public Card GetLastCard() => set[set.Count - 1];


    //add card beggining and end in two function called AddCardBeggining and AddCardEnd
    public void AddCardBeggining(Card card)
    {
        if (set.Count == 0)
        {
            set.Add(card);
            return;
        }
        set.Insert(0, card);
    }
    public void AddCardEnd(Card card)
    {
        if (set.Count == 0)
        {
            set.Add(card);
            return;
        }
        set.Add(card);
    }
    
    // can add at the begginging of the set of List<Card> return bool true it check if the postion of the card colum + 1 is equal to the first card postion colum
    public bool CanAddCardBeggining(Card card)
    {
        return GetFirstCard().Position.Column == card.Position.Column + 1;
    }
    // can add at the end of the set of List<Card> return bool true it check if the postion of the card colum - 1 is equal to the last card postion colum
    public bool CanAddCardEnd(Card card)
    {
        return GetLastCard().Position.Column == card.Position.Column - 1;
    }
    public bool IsContainsCard(Card card)
    {
        return set.Contains(card);
    }
    public CardsSet Combine(CardsSet set1, CardsSet set2)
    {
        foreach (Card c in set2.set)
        {
            set1.set.Add(c);
        }
        set1.set.Sort((x, y) => x.Position.Column.CompareTo(y.Position.Column));
        set2.set.Clear();
        return set1;
    }
    public CardsSet UnCombine(int offset)
    {
        CardsSet newSet = new CardsSet();
        for (int i = 0; i < offset; i++)
        {
            newSet.set.Add(set[i]);
        }
        set.RemoveRange(0, offset);
        return newSet;
    }


    public int RemoveCard(Card card)
    {
        int i = set.FindIndex(c => c == card);
        set.Remove(card);
        return i;
    }
    

    // Check if a run is valid
    public bool IsRun()
    {
        if (set == null || set.Count < 3 || set.Count > 13)
        {
            return false; // A run must have at least 3 cards but no more than 13
        }
        int i = GetFirstIndexOfNotJoker();

        CardColor SetColor = set[i].Color;
        int CurrentNum = set[i].Number;
        if (set[i].Number < i + 1) 
            return false;
        
        for (; i < set.Count; i++)
        {
            if ( CurrentNum == 14 || // the number is bigger than 13
                (CurrentNum & set[i].Number) != CurrentNum++|| // the number is not in sequence (we also check for jokers = 0xf)
                !isSameColor(set[i], SetColor)) // the color is not the same
            {
                return false;
            }
        }
        return true;
    }
    // Check if a group of colors is valid
    public bool IsGroupOfColors()
    {
        if (set == null || (set.Count != 3 && set.Count != 4))
        {
            return false; // A group must have either 3 or 4 cards
        }

        int i = GetFirstIndexOfNotJoker();
        int CurrentNum = set[i].Number;
        // Use a HashSet to track distinct colors
        HashSet<int> distinctColors = new HashSet<int>();

        for (; i < set.Count; i++)
        {
            if (!distinctColors.Add((int)set[i].Color) || (CurrentNum & set[i].Number) != CurrentNum)
            {
                return false; // If a color is repeated, the group is invalid
            }
        }

        return true;
    }
    public bool isSameColor(Card c1,CardColor color)
    {
        return IsJoker(c1) || c1.Color == color;
    }
    public bool IsJoker(Card card)
    {
        return card.Number == 0xf; // the joker is a mask of 1111b
    }



    public int GetFirstIndexOfNotJoker()
    {
        for (int i = 0; i < 3; i++)
        {
            if (!IsJoker(set[i]))
            {
                return i;
            }
        }

        return 2; // Default to the last index if all are jokers
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

    public int CountJokers()
    {
        int count = 0;
        foreach (Card card in set)
        {
            if (IsJoker(card))
                count++;
        }
        return count;
    }


}
