using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    private int number; // jokers =14?
    private CardColor color;

    public int Number { get => number; set => number = value; }
    public CardColor Color { get => color; set => color = value; }

    // Constructor that takes two arguments
    public Card(int number, CardColor color)
    {
        this.number = number;
        this.color = color;
    }

}
