
using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    private int turn = Constants.HumanTurn; // 0 = Human, 1 = Computer

    // Define a delegate for turn change event
    public delegate void TurnChangedEventHandler(int newTurn);
    // Define the event based on the delegate
    public event TurnChangedEventHandler TurnChanged;
    [SerializeField] private GameBoard board;
    public void ChangeTurn() // we call this function when the game is valid
    {
        this.turn ^= Constants.ChangeTurn; ; // 0 = 0^1 = 1, 1 = 1^1 = 0
        CheckGameState();
        // Trigger the turn change event
        OnTurnChanged(this.turn);
    }
    // Method to raise the event
    protected virtual void OnTurnChanged(int newTurn)
    {
        // Check if there are subscribers to the event
        if (TurnChanged != null)
        {
            // Call the event
            TurnChanged(newTurn);
        }
    }
    public int GetTurn() => this.turn;

    public void CheckGameState()
    {
        CheckWinner();
    }
    public void CheckWinner()
    {
        if (board.GetHumanHand().Count == Constants.EmptyStack)
        {
            Debug.Log("Human wins!");
        }
        else if (board.GetComputerHand().Count == Constants.EmptyStack)
        {
            Debug.Log("Computer wins!");
        }
    }

}
