using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHand : IEnumerable<Card>
{
  private readonly LinkedList<Card>[,] cardMatrix; // 2D array to store cards by color and number

  // Initialize the 2D array, O(n) where n is the number of colors and ranks
  public PlayerHand()
  {
    cardMatrix = new LinkedList<Card>[Constants.MaxSuit, Constants.MaxRank + 1]; // Increase size for Joker cards
    for (int i = 0; i < Constants.MaxSuit; i++) // iterate over colors and ranks to initialize the 2D array
      for (int j = 0; j < Constants.MaxRank + 1; j++) // Increase size for Joker cards
        cardMatrix[i, j] = new LinkedList<Card>();
  }
  // O(1)
  public void AddCard(Card card)
  {
    int colorIndex = (int)card.Color; // Get the color index
    int numberIndex = card.Number - 1; // Get the number index
    // Handle Joker cards
    if (card.Number == Constants.JokerRank) 
    {
      numberIndex = Constants.MaxRank; // Store Joker cards at the last index
    }
    cardMatrix[colorIndex, numberIndex].AddLast(card); // Add the card to the 2D array
  }

  // O(n) where n is the number of cards in the hand
  public List<Card> SortedByRun()
  {
    List<Card> sortedCards = new List<Card>(); // Create a list to store the sorted cards
    for (int i = 0; i < Constants.MaxSuit; i++) // Iterate over the colors, 0-3
      for (int j = 0; j < Constants.MaxRank + 1; j++) // Iterate over the ranks, 0-13
        foreach (Card card in cardMatrix[i, j]) // Iterate over the cards in the 2D array, 0-1
          sortedCards.Add(card); // Add the card to the list
    return sortedCards;
  }
  // O(n) where n is the number of cards in the hand
  public List<Card> SortedByGroup()
  {
    List<Card> sortedCards = new List<Card>(); // Create a list to store the sorted cards
    for (int j = 0; j < Constants.MaxRank + 1; j++) // Iterate over the ranks, 0-13
      for (int i = 0; i < Constants.MaxSuit; i++) // Iterate over the colors, 0-3
        foreach (Card card in cardMatrix[i, j]) // Iterate over the cards in the 2D array, 0-1
          sortedCards.Add(card); // Add the card to the list
    return sortedCards;
  }

  // remove card from hand, O(1)
  public void RemoveCard(Card card)
  {
    int colorIndex = (int)card.Color; // Get the color index
    int numberIndex = card.Number - 1; // Get the number index
    if (card.Number == Constants.JokerRank) // Handle Joker cards
    {
      numberIndex = Constants.MaxRank; // Store Joker cards at the last index
    }
    if (cardMatrix[colorIndex, numberIndex].Count == 0) //invalid moment remoce later
    {
      throw new System.Exception("No such card in hand. "+ card.ToString() + " is not in hand.");
    }
    cardMatrix[colorIndex, numberIndex].Remove(card); // Remove the card from the 2D array, O(1)
  }

  // if the hand contains the card, O(1)
  public bool Contains(Card card)
  {
    int colorIndex = (int)card.Color; // Get the color index
    int numberIndex = card.Number - 1; // Get the number index
    return cardMatrix[colorIndex, numberIndex].Count > 0; // Check if the card is in the 2D array, O(1)
  }

 // on demand, O(1), used for, foreach (Card card in playerHand) which is O(n)
  public IEnumerator<Card> GetEnumerator()
  {
    for (int i = 0; i < Constants.MaxSuit; i++) // Iterate over the colors
      for (int j = 0; j < Constants.MaxRank; j++) // Iterate over the ranks
        foreach (Card card in cardMatrix[i, j]) // Iterate over the cards in the 2D array 
          yield return card; // Return the card
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  // print the cards in the hand, O(n) where n is the number of cards in the hand
  public override string ToString()
  {
    Debug.Log("Print cards:");
    string txt = "\n";
    for (int i = 0; i < Constants.MaxSuit; i++) // Iterate over the colors
    {
      for (int j = 0; j < Constants.MaxRank + 1; j++) // Iterate over the ranks
      {
        foreach (Card card in cardMatrix[i, j]) // Iterate over the cards in the 2D array
        {
          txt += card.ToString() + ", "; // Print the card separated by a comma
        }
      }
      Debug.Log(txt);
      txt = "\n"; // Move to the next line for the next color
    }
    return txt;
  }
  // Equals method, O(1)
    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj);
    }
}
