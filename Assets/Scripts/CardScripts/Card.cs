using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

   // Represents a card on the board
public class Card : MonoBehaviour
{
    private int number; // 1-13
    private CardColor color; // Red, Blue, Yellow, Black
    private CardPosition position; // Row and Column
    public int Number { get => number; set => number = value; }
    public CardColor Color { get => color; set => color = value; }
    public CardPosition Position { get => position; set => position = value; }

    // Constructor that takes two arguments
    public Card(int number, CardColor color)
    {
        this.number = number;
        this.color = color;
    }

}
