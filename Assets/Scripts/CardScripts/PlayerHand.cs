using System.Collections;
using System.Collections.Generic;

public class PlayerHand : IEnumerable<Card>
{
  private readonly LinkedList<Card>[,] cardMatrix; // 2D array to store cards by color and number

  // Number of buckets per color: ranks 1-13 plus one extra bucket for Jokers
  private const int BucketsPerColor = Constants.MaxRank + 1;

  // Total number of cards currently in the hand, maintained by AddCard/RemoveCard, O(1)
  public int Count { get; private set; }

  // Initialize the 2D array, O(n) where n is the number of colors and ranks
  public PlayerHand()
  {
    cardMatrix = new LinkedList<Card>[Constants.MaxSuit, BucketsPerColor];
    for (int i = 0; i < Constants.MaxSuit; i++) // iterate over colors and ranks to initialize the 2D array
      for (int j = 0; j < BucketsPerColor; j++)
        cardMatrix[i, j] = new LinkedList<Card>();
  }

  // Jokers are stored in the extra bucket at the end of each color row
  private static int GetNumberIndex(Card card) =>
    card.Number == Constants.JokerRank ? Constants.MaxRank : card.Number - 1;

  // O(1)
  public void AddCard(Card card)
  {
    cardMatrix[(int)card.Color, GetNumberIndex(card)].AddLast(card); // Add the card to the 2D array
    Count++;
  }

  // O(n) where n is the number of cards in the hand
  public List<Card> SortedByRun()
  {
    List<Card> sortedCards = new List<Card>(Count); // Create a list to store the sorted cards
    for (int i = 0; i < Constants.MaxSuit; i++) // Iterate over the colors, 0-3
      for (int j = 0; j < BucketsPerColor; j++) // Iterate over the ranks, then jokers
        foreach (Card card in cardMatrix[i, j]) // Iterate over the cards in the bucket, 0-2
          sortedCards.Add(card); // Add the card to the list
    return sortedCards;
  }
  // O(n) where n is the number of cards in the hand
  public List<Card> SortedByGroup()
  {
    List<Card> sortedCards = new List<Card>(Count); // Create a list to store the sorted cards
    for (int j = 0; j < BucketsPerColor; j++) // Iterate over the ranks, then jokers
      for (int i = 0; i < Constants.MaxSuit; i++) // Iterate over the colors, 0-3
        foreach (Card card in cardMatrix[i, j]) // Iterate over the cards in the bucket, 0-2
          sortedCards.Add(card); // Add the card to the list
    return sortedCards;
  }

  // remove card from hand, O(1)
  public void RemoveCard(Card card)
  {
    LinkedList<Card> bucket = cardMatrix[(int)card.Color, GetNumberIndex(card)];
    if (!bucket.Remove(card)) // Remove exactly this card instance from its bucket
      throw new System.Exception("No such card in hand. " + card + " is not in hand.");
    Count--;
  }
  //function O(1) get joker else null either black or either red
  public Card GetJoker()
  {
    int numberIndex = Constants.MaxRank; // Get the number index for Joker
    if (cardMatrix[(int)CardColor.Black, numberIndex].Count > 0) // Check if the black Joker is in the 2D array
      return cardMatrix[(int)CardColor.Black, numberIndex].First.Value; // Return the black Joker
    if (cardMatrix[(int)CardColor.Red, numberIndex].Count > 0) // Check if the red Joker is in the 2D array
      return cardMatrix[(int)CardColor.Red, numberIndex].First.Value; // Return the red Joker
    return null; // Return null if no Joker is found
  }
  // if the hand contains this exact card instance, O(1)
  // (the deck holds two identical copies of every tile, so identity matters - not just color/number)
  public bool Contains(Card card)
  {
    return cardMatrix[(int)card.Color, GetNumberIndex(card)].Contains(card);
  }

 // on demand, O(1), used for, foreach (Card card in playerHand) which is O(n)
  public IEnumerator<Card> GetEnumerator()
  {
    for (int i = 0; i < Constants.MaxSuit; i++) // Iterate over the colors
      for (int j = 0; j < BucketsPerColor; j++) // Iterate over the ranks, then jokers
        foreach (Card card in cardMatrix[i, j]) // Iterate over the cards in the bucket
          yield return card; // Return the card
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  // print the cards in the hand, O(n) where n is the number of cards in the hand
  public override string ToString()
  {
    System.Text.StringBuilder txt = new System.Text.StringBuilder();
    foreach (Card card in this)
      txt.Append(card).Append(", ");
    return txt.ToString();
  }
}
