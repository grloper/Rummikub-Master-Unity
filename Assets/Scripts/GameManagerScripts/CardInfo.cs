using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInfo 
{
    Card card;
    SetPosition setPosition;
    AddPosition position;
    int cardIndex;
    public CardInfo(Card card, SetPosition setPosition, AddPosition position, int cardIndex = -1)
    {
        this.card = card;
        this.setPosition = setPosition;
        this.cardIndex = cardIndex;
         this.position = position;
    }
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
