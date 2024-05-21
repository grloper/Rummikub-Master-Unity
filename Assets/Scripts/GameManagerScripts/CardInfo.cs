using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardInfo 
{
    Card card;
    SetPosition setPosition;
    bool isEnd;
    int cardIndex;
    public CardInfo(Card card, SetPosition setPosition, bool isEnd, int cardIndex = -1)
    {
        this.card = card;
        this.setPosition = setPosition;
        this.isEnd = isEnd;
        this.cardIndex = cardIndex;
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
    public bool IsEnd()
    {
        return this.isEnd;
    }
    public void SetIsEnd(bool isEnd)
    {
        this.isEnd = isEnd;
    }
    public void SetSetPosition(SetPosition setPosition)
    {
        this.setPosition = setPosition;
    }
    public void SetCard(Card card)
    {
        this.card = card;
    }
    public override string ToString()
    {
        return "Card: " + card.ToString() + " SetPosition: " + setPosition.ToString() + " IsEnd: " + isEnd + " CardIndex: " + cardIndex;
    }



}
