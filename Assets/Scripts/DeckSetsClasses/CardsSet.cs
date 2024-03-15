using System;
using System.Collections;
using System.Collections.Generic;
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
    public Card FirstCard() => set[0];

    public Card LastCard() => set[set.Count - 1];


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
        return FirstCard().Position.Column == card.Position.Column + 1;
    }
    // can add at the end of the set of List<Card> return bool true it check if the postion of the card colum - 1 is equal to the last card postion colum
    public bool CanAddCardEnd(Card card)
    {
        return LastCard().Position.Column == card.Position.Column - 1;
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

    public void RemoveCard(Card card)
    {
        set.Remove(card);
    }

    public bool IsRun()
    {
        CardColor firstCardColor = set[0].Color;

        for (int i = 1; i < set.Count; i++)
        {
            if (set[i].Number == 14 || set[i - 1].Number == 14)
            {
                continue;
            }

            if (set[i].Number != set[i - 1].Number + 1 || set[i].Color != firstCardColor)
            {
                return false;
            }
        }

        return set.Count >= 3 && set.Count <= 13;
    }

    public bool IsGroupOfColors()
    {
        int firstCardNumber = set[0].Number;
        HashSet<CardColor> uniqueColors = new HashSet<CardColor>();

        foreach (Card card in set)
        {
            if (card.Number == 14)
            {
                continue;
            }
            if (card.Number != firstCardNumber)
            {
                return false;
            }

            if (!uniqueColors.Add(card.Color))
            {
                return false;
            }
        }

        return set.Count >= 3 && set.Count <= 4;
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
}
