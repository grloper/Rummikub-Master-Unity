using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Human : Player
{
    [SerializeField] GameObject HumanGrid;
    // Reference to UImanager
    [SerializeField] private UImanager uiManager;
    [SerializeField] private GameBoard board;

    // Start is called before the first frame update
    void Start()
    {

    }

    private void InitHumanDeck()
    {
        // Draw random cards and assign them to 14 slots on the Human board
        for (int i = 0; i < 14; i++)
        {
            GameObject tileSlot = HumanGrid.transform.GetChild(i).gameObject;
            // Draw a random card from the deck using RummikubDeck
            Card randomCard = uiManager.InstinitanteCard(board.GetRummikubDeckInstance().DrawRandomCardFromDeck(), tileSlot);
            board.AddCardToHumanHand(randomCard); // Add the drawn card to the humanDeck in the GameBoard.cs script
            if (randomCard == null)
            {
                Debug.LogWarning("Unable to draw a card for the Human's board.");
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private int GetEmptySlotIndex()
    {
        for (int i = 0; i < HumanGrid.transform.childCount; i++)
        {
            GameObject tileSlot = HumanGrid.transform.GetChild(i).gameObject;

            // Check if the slot is empty
            if (tileSlot.transform.childCount == 0)
            {
                // Return the index of the empty slot
                return i;
            }
        }

        // Return -1 if no empty slot is found
        return -1;
    }

    public override void DrawCard()
    {
        // Get the index of the first empty slot
        int emptySlotIndex = GetEmptySlotIndex();

        // Check if an empty slot is found
        if (emptySlotIndex != -1)
        {
            try
            {
                GameObject tileSlot = HumanGrid.transform.GetChild(emptySlotIndex).gameObject;
                // Draw a random card from the deck using RummikubDeck
                Card randomCard = uiManager.InstinitanteCard(board.GetRummikubDeckInstance().DrawRandomCardFromDeck(), tileSlot);
                board.AddCardToHumanHand(randomCard); // Add the drawn card to the humanDeck in the GameBoard.cs script
                print("Human hand: "+board.GetHumanHand().Count);
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning("Deck is Empty.");
            }
        }
        else
        {
            Debug.LogWarning("No empty slots available.");
            // Handle the case where no empty slots are found, perhaps prompt the user or handle it accordingly.
        }
    }


    public override void InitBoard()
    {
        // Connect with UImanager
        if (uiManager == null)
        {
            Debug.LogError("UImanager reference is not set in the Inspector!");
            // return;
        }
        InitHumanDeck();
    }
}
