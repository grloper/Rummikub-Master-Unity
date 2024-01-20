using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : Player
{
    // Reference to UImanager
    [SerializeField] private UImanager uiManager;
    [SerializeField] private GameBoard board;
    public override void DrawCard()
    {
    }

    public override void InitBoard()
    {
        // Connect with UImanager
        if (uiManager == null)
        {
            Debug.LogError("UImanager reference is not set in the Inspector!");
            // return;
        }
        //InitComputerDeck();
    }

    //private void InitComputerDeck()
    //{
    //    // Draw random cards and assign them to 14 slots on the Human board
    //    for (int i = 0; i < 14; i++)
    //    {
    //        // Draw a random card from the deck using RummikubDeck
    //        Card randomCard = uiManager.InstinitanteCard(board.GetRummikubInstance().DrawRandomCardFromDeck(), tileSlot);
    //        board.AddCardToHumanHand(randomCard); // Add the drawn card to the humanDeck in the GameBoard.cs script
    //        if (randomCard == null)
    //        {
    //            Debug.LogWarning("Unable to draw a card for the Human's board.");
    //        }
    //    }
    //}

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
