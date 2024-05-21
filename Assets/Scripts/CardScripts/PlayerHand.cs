using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHand : IEnumerable<Card>
{
  private LinkedList<Card>[,] cardMatrix;

  public PlayerHand()
  {
    cardMatrix = new LinkedList<Card>[Constants.MaxSuit, Constants.MaxRank + 1]; // Increase size for Joker cards
    for (int i = 0; i < Constants.MaxSuit; i++)
    {
      for (int j = 0; j < Constants.MaxRank + 1; j++) // Increase size for Joker cards
      {
        cardMatrix[i, j] = new LinkedList<Card>();
      }
    }
  }
  public void AddCard(Card card)
  {
    int colorIndex = (int)card.Color;
    int numberIndex = card.Number - 1;

    // Handle Joker cards
    if (card.Number == Constants.JokerRank)
    {
      numberIndex = Constants.MaxRank; // Store Joker cards at the last index
    }
    cardMatrix[colorIndex, numberIndex].AddLast(card);
  }

  public List<Card> SortedByRun()
  {
    List<Card> sortedCards = new List<Card>();
    for (int i = 0; i < Constants.MaxSuit; i++)
    {
      for (int j = 0; j < Constants.MaxRank + 1; j++)
      {
        foreach (Card card in cardMatrix[i, j])
        {
          sortedCards.Add(card);
        }
      }
    }
    return sortedCards;
  }
  public List<Card> SortedByGroup()
  {
    List<Card> sortedCards = new List<Card>();
    for (int j = 0; j < Constants.MaxRank + 1; j++)
    {
      for (int i = 0; i < Constants.MaxSuit; i++)
      {
        foreach (Card card in cardMatrix[i, j])
        {
          sortedCards.Add(card);
        }
      }
    }
    return sortedCards;
  }

  public void RemoveCard(Card card)
  {
    int colorIndex = (int)card.Color;
    int numberIndex = card.Number - 1;
    if (card.Number == Constants.JokerRank)
    {
      numberIndex = Constants.MaxRank; // Store Joker cards at the last index
    }

    if (cardMatrix[colorIndex, numberIndex].Count == 0)
    {
      throw new System.Exception("No such card in hand. "+ card.ToString() + " is not in hand.");
    }
    cardMatrix[colorIndex, numberIndex].Remove(card);
  }

  public bool Contains(Card card)
  {
    int colorIndex = (int)card.Color;
    int numberIndex = card.Number - 1;

    return cardMatrix[colorIndex, numberIndex].Count > 0;
  }


  public IEnumerator<Card> GetEnumerator()
  {
    for (int i = 0; i < Constants.MaxSuit; i++)
    {
      for (int j = 0; j < Constants.MaxRank; j++)
      {
        foreach (Card card in cardMatrix[i, j])
        {
          yield return card;
        }
      }
    }
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  public override string ToString()
  {
    Debug.Log("Print cards:");
    string txt = "\n";
    for (int i = 0; i < Constants.MaxSuit; i++)
    {
      for (int j = 0; j < Constants.MaxRank + 1; j++)
      {
        foreach (Card card in cardMatrix[i, j])
        {
          txt += card.ToString() + ", ";
        }
      }
      Debug.Log(txt);
      txt = "\n";
    }
    return txt;
  }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj);
    }

}
