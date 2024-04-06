using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardComparer : IComparer<Card>
{
    private Card previousCard = null;

    public int Compare(Card card1, Card card2)
    {
        if (card1.Color == card2.Color)
        {
            // Check for consecutive card of the same color
            if (previousCard != null && previousCard.Color == card1.Color && card2.Number == previousCard.Number + 1)
            {
                // Swap to avoid consecutive cards
                previousCard = card2;
                return 1;
            }
            previousCard = card1;
            return card1.Number.CompareTo(card2.Number);
        }
        else
        {
            previousCard = null;
            return card1.Color.CompareTo(card2.Color);
        }
    }
}