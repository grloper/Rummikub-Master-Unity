using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICardSet
{
    bool IsRun();
    bool IsGroupOfColors();
    bool IsContainsCard(Card card);
    Card GetFirstCard();
    Card GetLastCard();
    void AddCardBeggining(Card card);
    void AddCardEnd(Card card);
    bool IsSameColor(Card c1, CardColor color);
    bool IsConsicutive(Card c1, Card c2);


}
