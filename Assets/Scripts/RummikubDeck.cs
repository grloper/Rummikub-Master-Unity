using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RummikubDeck
{
    private List<Card> deck = new List<Card>();
    public RummikubDeck()
    {
        InitializeDeck();
    }
    private void InitializeDeck()
    {
            for (int i = 1; i <= 13; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    CardColor color = (CardColor)j;
                    Card card = new Card(i, color);
                    deck.Add(card);
                    deck.Add(card); // Adding a second set
                }
            }

            // Adding jokers manually
            deck.Add(new Card(14, CardColor.Red));
            deck.Add(new Card(14, CardColor.Black));
    }
    public Card DrawRandomCardFromDeck()
    {
        if (deck.Count == 0)
        {
            Debug.LogWarning("Deck is empty!");
            return null;
        }

        int randomIndex = Random.Range(0, deck.Count);
        Card drawnCard = deck[randomIndex];
        deck.RemoveAt(randomIndex);
        return drawnCard;
    }
}
