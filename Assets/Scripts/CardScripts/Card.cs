
using UnityEngine;

// Represents a card on the board
public class Card : MonoBehaviour
{
    private int number; // 1-13, joker = 15 = 1111b = 0xf
    private CardColor color; // Red, Blue, Black, Orange
    private CardPosition position; // Row and Column of the card after it was moved
    private CardPosition oldPosition; // Row and Column of the card before it was moved
    private bool cameFromPlayerHand; // True if the card was moved from the player hand to the board false if from board to board
    private Transform parentBeforeDrag; // The parent of the card before it was dragged so we can undo the drag if needed to the first postion and only to the last
    private CardPosition oldPositionBeforeDrag; // The set position of the card before it was dragged so we can undo the drag if needed to the first postion and only to the last
    // Gets and Sets
    public int Number { get => number; set => number = value; } // get and set the number of the card
    public CardColor Color { get => color; set => color = value; } // get and set the color of the card
    public CardPosition Position { get => position; set => position = value; } // get and set the position of the card
    public CardPosition OldPosition { get => oldPosition; set => oldPosition = value; } // get and set the old position of the card
   
    // Undo Parameters and for logics.
    public bool CameFromPlayerHand { get => cameFromPlayerHand; set => cameFromPlayerHand = value; } // get and set if the card came from the player hand
    public Transform ParentBeforeDrag { get => parentBeforeDrag; set => parentBeforeDrag = value; } // get and set the parent of the card before it was dragged
    public CardPosition OldPositionBeforeDrag { get => oldPositionBeforeDrag; set => oldPositionBeforeDrag = value; } // get and set the old position of the card before it was dragged
    public Card()
    {
        // Default constructor
    }

    // Constructor that takes two arguments
    public Card(int number, CardColor color)
    {
        this.number = number;
        this.color = color;
        OldPosition = new CardPosition();
        Position = new CardPosition();
    }
    // ToString, Equals 
    public override string ToString()
    {
        // Return a string representation of the card
        return "Card: <color=" + color.ToString().ToLower() + ">" + number + "</color>("+((position.Row*100)+position.Column)+")";// + position.Row + " X:" + position.Column;
    }
    // Every card is unique, so we can compare them by reference
    public override bool Equals(object other)
    {
        // Check for reference equality
        return ReferenceEquals(this, other);
    }

}



