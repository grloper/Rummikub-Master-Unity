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
    private bool cameFromHumanHand; // True if the card was moved from the human hand to the board false if from board to board
    private Transform parentBeforeDrag; // The parent of the card before it was dragged

    public int Number { get => number; set => number = value; }
    public CardColor Color { get => color; set => color = value; }
    public CardPosition Position { get => position; set => position = value; }
    public CardPosition OldPosition { get => position; set => position = value; }
    public bool CameFromHumanHand { get => cameFromHumanHand; set => cameFromHumanHand = value; }
    public Transform ParentBeforeDrag { get => parentBeforeDrag; set => parentBeforeDrag = value; }
    public Card()
    {
        // Default constructor
    }

    // Constructor that takes two arguments
    public Card(int number, CardColor color)
    {
        
        this.number = number;
        this.color = color;
    }

    public override string ToString()
    {
        //return all values
        return "Card: " + number + " " + color + "Y:" + position.Row + ",X:" + position.Column;
    }
}

