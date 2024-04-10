using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardComparer : IComparer<Card>
{
    private List<Card> hand;

    public CardComparer(List<Card> hand)
    {
        this.hand = hand;
    }

    public int Compare(Card x, Card y)
    {
        // Step 1: Sort by Color
        if (x.Color != y.Color)
        {
            return x.Color.CompareTo(y.Color);
        }

        // Step 2: Sort by Number (within color)
        int numberComparison = x.Number.CompareTo(y.Number);
        if (numberComparison != 0)
        {
            return numberComparison;
        }

        // Step 3: Separate Runs from Non-Runs (within color and number)
        bool isXInRun = CanBePartOfRun(x, hand);
        bool isYInRun = CanBePartOfRun(y, hand);

        // If only one is in a run, sort the one not in the run first
        if (isXInRun != isYInRun)
        {
            return isXInRun ? 1 : -1;
        }

        // Step 4: Separate Runs with Duplicates from Other Cards (within color, number, and run status)
        // Check if either card is part of a run with duplicates
        bool isXInRunWithDuplicates = IsRunWithDuplicates(x, hand);
        bool isYInRunWithDuplicates = IsRunWithDuplicates(y, hand);

        if (isXInRunWithDuplicates != isYInRunWithDuplicates)
        {
            return isXInRunWithDuplicates ? 1 : -1;
        }

        // Step 5: Sort Duplicates Last (within color, number, run status, and with/without duplicates)
        // If both cards have the same color, number, run status, and run with duplicates status:
        if (isXInRunWithDuplicates == isYInRunWithDuplicates)
        {
            // Then check for duplicates:
            bool isXDuplicate = hand.Count(c => c.Color == x.Color && c.Number == x.Number) > 1;
            bool isYDuplicate = hand.Count(c => c.Color == y.Color && c.Number == y.Number) > 1;

            // Place the duplicate card last:
            if (isXDuplicate != isYDuplicate)
            {
                return isXDuplicate ? 1 : -1;
            }
        }
        return 0; // Assuming duplicates of the same number and color should be grouped together
    }

    private bool CanBePartOfRun(Card card, List<Card> hand)
    {
        // Check if the card's number could be part of a valid run (up to 3) in the hand
        int potentialRunLength = 1;
        foreach (Card otherCard in hand)
        {
            if (card.Color == otherCard.Color && card.Number == otherCard.Number - potentialRunLength)
            {
                potentialRunLength++;
                if (potentialRunLength == 3)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsRunWithDuplicates(Card card, List<Card> hand)
    {
        // Check if the card is part of a run with at least one duplicate
        int potentialRunLength = 1;
        int currentNumber = card.Number;
        foreach (Card otherCard in hand)
        {
            if (otherCard.Color == card.Color && otherCard.Number == currentNumber)
            {
                potentialRunLength++;
                currentNumber--;
                if (potentialRunLength > 1)
                {
                    return true;
                }
            }
            else
            {
                currentNumber = card.Number;
                potentialRunLength = 1;
            }
        }
        return false;
    }

}
