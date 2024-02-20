using System;

using UnityEngine;

public class Computer : Player
{
    // Reference to UImanager
    [SerializeField] private UImanager uiManager;
    [SerializeField] private GameBoard board;
    public override void DrawCard()
    {
        try
        {
            // Draw a random card from the deck using RummikubDeck via the GameBoard instance
            Card randomCard = uiManager.InstinitanteCard(board.GetRummikubDeckInstance().DrawRandomCardFromDeck());
            board.AddCardToComputerHand(randomCard); // Add the drawn card to the computerDeck in the GameBoard.cs script
            print("Computer hand: "+board.GetComputerHand().Count);
        }
        catch (EmptyDeckException)
        {
            Debug.LogWarning("Deck is Empty.");
        }
    }
    public bool GetInitialMove()
    {
        return initialMove;
    }

    public void SetInitialMove(bool value)
    {
        initialMove = value;
    }
    public override void InitBoard()
    {
        // Connect with UImanager
        if (uiManager == null)
        {
            Debug.LogError("UImanager reference is not set in the Inspector!");
            // return;
        }
        InitComputerDeck();
    }


    private void InitComputerDeck()
    {
        // Draw random cards and assign them to 14 slots on the Human board
        for (int i = 0; i < Constants.CardsToDraw; i++)
        {
            // Draw a random card from the deck using RummikubDeck
            Card randomCard = uiManager.InstinitanteCard(board.GetRummikubDeckInstance().DrawRandomCardFromDeck());
            board.AddCardToComputerHand(randomCard); // Add the drawn card to the humanDeck in the GameBoard.cs script
            if (randomCard == null)
            {
                Debug.LogWarning("Unable to draw a card for the Computer's Hand.");
            }
        }
    }


}
