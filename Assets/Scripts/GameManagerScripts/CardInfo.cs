using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class is used to store the card information, such as the card itself, the position of the card, the index of the card, and the set position of the card.
// this is a card we are going to add to a partial set. in order to create a valid 3 length set.
public class CardInfo 
{
    Card card; // the card itself
    SetPosition setPosition; // the location of set where the card is being extracted from
    AddPosition position; // position where the card being extracted from
    int cardIndex; // index of the card in the set
    // Constructor for the CardInfo class
    public CardInfo(Card card, SetPosition setPosition, AddPosition position, int cardIndex = -1)
    {
        this.card = card;
        this.setPosition = setPosition;
        this.cardIndex = cardIndex;
        this.position = position;
    }
    // Getters and Setters for the CardInfo class
    public AddPosition GetPosition()
    {
        return this.position;
    }
    public void SetPosition(AddPosition position)
    {
        this.position = position;
    }
    public int GetCardIndex()
    {
        return this.cardIndex;
    }
    public void SetCardIndex(int cardIndex)
    {
        this.cardIndex = cardIndex;
    }
    public Card GetCard()
    {
        return this.card;
    }
    public SetPosition GetSetPosition()
    {
        return this.setPosition;
    }

    public void SetSetPosition(SetPosition setPosition)
    {
        this.setPosition = setPosition;
    }
    public void SetCard(Card card)
    {
        this.card = card;
    }

}
