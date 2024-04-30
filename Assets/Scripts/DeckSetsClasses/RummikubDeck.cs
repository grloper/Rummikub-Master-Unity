using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
public class RummikubDeck
{
    private readonly List<Card> deck = new List<Card>();
    public RummikubDeck()
    {
        InitializeDeck();
    }
    private void InitializeDeck()
    {
        for (int i = 1; i <= Constants.MaxRank; i++)
        {
            for (int j = 0; j < Constants.MaxSuit; j++)
            {
                //Card c2 = new Card(i, (CardColor)j);
                deck.Add(new Card(i, (CardColor)j));
                //Card c = new Card(i, (CardColor)j);
                deck.Add(new Card(i, (CardColor)j)); // Adding a second set

            }
        }

        // Adding jokers manually
        deck.Add(new Card(Constants.JokerRank, CardColor.Red));
        deck.Add(new Card(Constants.JokerRank, CardColor.Black));
    }
    public Card DrawRandomCardFromDeck()
    {
        if (deck.Count == Constants.EmptyDeck)
        {
            throw new EmptyDeckException();
        }
        int randomIndex = Random.Range(0, deck.Count);
        Card drawnCard = deck[randomIndex];
        deck.RemoveAt(randomIndex);
        return drawnCard;
    }

    // Get the length of the deck

    public int GetDeckLength()
    {
        return deck.Count;
    }
}
