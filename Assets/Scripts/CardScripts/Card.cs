
using TMPro;
using UnityEngine;

// Represents a card on the board
public class Card : MonoBehaviour
{
    private int number; // 1-13, joker = 15 = 1111b = 0xf
    private CardColor color; // Red, Blue, Black, Orange
    private CardPosition position; // Row and Column of the card after it was moved
    private CardPosition oldPosition; // Row and Column of the card before it was moved
    private bool cameFromHumanHand; // True if the card was moved from the human hand to the board false if from board to board
    private Transform parentBeforeDrag; // The parent of the card before it was dragged so we can undo the drag if needed to the first postion and only to the last
    // Gets and Sets
    public int Number { get => number; set => number = value; }
    public CardColor Color { get => color; set => color = value; }
    public CardPosition Position { get => position; set => position = value; }
    public CardPosition OldPosition { get => oldPosition; set => oldPosition = value; }
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
        OldPosition = new CardPosition();
        Position = new CardPosition();
    }

    public override string ToString()
    {
        // Return a string representation of the card
        return "Card: " + number + " " + color + "Y:" + position.Row + " X:" + position.Column;

    }
}

