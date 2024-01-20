using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RummikubDeck
{
    private List<Card> initializedDeck = new List<Card>();
    public RummikubDeck()
    {
        InitializeDeck();
    }
    private void InitializeDeck()
    {
        //106 tiles at deck
        initializedDeck.Clear();
        //loop number
            for (int i = 1; i <= 13; i++)
            {
            //loop color
                for (int j = 0; j < 4; j++)
                {
                    CardColor color = (CardColor)j;
                    Card card = new Card(i, color);
                //two sets of each card
                initializedDeck.Add(card);
                initializedDeck.Add(card); 
                }
            }
            // Adding jokers manually
            initializedDeck.Add(new Card(14, CardColor.Red));
            initializedDeck.Add(new Card(14, CardColor.Black));
    }
    public int GetDeckLength()
    {
        return initializedDeck.Count;
    }
    public Card DrawRandomCardFromDeck() 
    {
        
        if (initializedDeck.Count == 0)
        {
            throw new NullReferenceException();
        }

        int randomIndex = Random.Range(0, initializedDeck.Count);
        Card drawnCard = initializedDeck[randomIndex];
        initializedDeck.RemoveAt(randomIndex);
        return drawnCard;
    }
}
