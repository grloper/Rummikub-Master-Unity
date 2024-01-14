using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitializeGame : MonoBehaviour
{
    //game Board
    [SerializeField] GameObject BoardGrid;
    [SerializeField] GameObject PlayerGrid;
    //game Tiles
    [SerializeField] GameObject TileSlotPrefab;
    [SerializeField] GameObject TileSlotPlayerPrefab;

    // Reference to UImanager
    public UImanager uiManager;
    public RummikubDeck rummikubDeck;  
    
    // Start is called before the first frame update
    void Start()
    {
        rummikubDeck = new RummikubDeck();
        // Connect with UImanager
        if (uiManager == null)
        {
            Debug.LogError("UImanager reference is not set in the Inspector!");
           // return;
        }
        //Empty Slot Creater
        Init();
    }

    private void Init()
    {
        // Initialize Game Board
        for (int i = 0; i < 168; i++)
        {
            Instantiate(TileSlotPrefab, BoardGrid.transform);
        }

        // Initialize Player Board with 44 slots
        for (int i = 0; i < 44; i++)
        {
            Instantiate(TileSlotPlayerPrefab, PlayerGrid.transform);
        }
        // Draw random cards and assign them to 14 slots on the player board
        for (int i = 0; i < 14; i++)
        {
            GameObject tileSlot = PlayerGrid.transform.GetChild(i).gameObject;

            // Draw a random card from the deck using RummikubDeck
            Card randomCard = uiManager.InstinitanteCard(rummikubDeck.DrawRandomCardFromDeck(), tileSlot);

            if (randomCard == null)
            {
                Debug.LogWarning("Unable to draw a card for the player's board.");
            }

        }


    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
