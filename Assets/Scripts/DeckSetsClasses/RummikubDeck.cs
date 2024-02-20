using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RummikubDeck
{
    // List of cards in the deck contains 106 cards at the start of the game
    private List<Card> initializedDeck = new List<Card>();
    public RummikubDeck()
    {
        InitializeDeck();
    }
    private void InitializeDeck()
    {
        initializedDeck.Clear();
        //loop to get every number
        for (int i = 1; i <= Constants.MaxRank; i++) //i= 1 - 13
        {
            //loop to get every color
            for (int j = 0; j < Constants.MaxSuit; j++) //j= 0 - 3
            {
                CardColor color = (CardColor)j;
                Card card = new Card(i, color);
                //two sets of each card
                initializedDeck.Add(card);
                initializedDeck.Add(card);

            }
        }
        // Adding jokers manually
        initializedDeck.Add(new Card(Constants.JokerRank, CardColor.Red));
        initializedDeck.Add(new Card(Constants.JokerRank, CardColor.Black));
    }
    // Get the length of the deck
    public int GetDeckLength()
    {
        return initializedDeck.Count;
    }
    // Draw a random card from the deck
    public Card DrawRandomCardFromDeck()
    {
        // Check if the deck is empty before drawing a card and throw an exception if it is
        if (initializedDeck.Count == Constants.EmptyDeck)
        {
            throw new EmptyDeckException();
        }
        // else draw a random card from the deck
        int randomIndex = Random.Range(0, initializedDeck.Count);
        Card drawnCard = initializedDeck[randomIndex];
        initializedDeck.RemoveAt(randomIndex);
        return drawnCard;
    }
   

}
