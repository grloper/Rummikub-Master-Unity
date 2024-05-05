using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand
{
    private readonly int[,] cardMatrix; // Matrix to store card appearances (rows: numbers, columns: colors)
    private readonly int[] cardAppearances; // Array to store the number of instances for each card

    public PlayerHand()
    {
        // Initialize the matrix and appearances array with appropriate sizes based on your card types
        cardMatrix = new int[Constants.MaxRank, Constants.MaxSuit];
        cardAppearances = new int[Constants.MaxRank * Constants.MaxSuit];
    }

    public void AddCard(Card card)
    {
        int colorIndex = (int)card.Color;
        int numberIndex = card.Number;

        // Increment the appearance count for this card
        cardAppearances[GetCardIndex(colorIndex, numberIndex)]++;

        // Update the matrix with the new appearance
        cardMatrix[numberIndex, colorIndex]++;
    }

    public void RemoveCard(Card card)
    {
        int colorIndex = (int)card.Color;
        int numberIndex = card.Number;

        // Decrement the appearance count for this card
        cardAppearances[GetCardIndex(colorIndex, numberIndex)]--;

        // Update the matrix with the reduced appearance
        cardMatrix[numberIndex, colorIndex]--;
    }

    private int GetCardIndex(int colorIndex, int numberIndex)
    {
        // This function combines the color and number indices into a single index for the cardAppearances array
        // You can adjust the calculation based on your specific card representation
        return colorIndex * 13 + numberIndex;
    }

    // Additional methods (optional):

    public bool ContainsCard(Card card)
    {
        return cardAppearances[GetCardIndex((int)card.Color, card.Number)] > 0;
    }

    public int GetCardCount(Card card)
    {
        return cardAppearances[GetCardIndex((int)card.Color, card.Number)];
    }
}
