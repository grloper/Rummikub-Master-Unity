using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Reference to UImanager
    [SerializeField] private UImanager uiManager;
    [SerializeField] protected GameBoard board;
    private GameObject PlayerGrid;
    [SerializeField] private PlayerType playerType;
    protected List<Card> playerHand;
    protected bool initialMove;

    // act as a constructor for the player because we are using : MonoBehaviour
    public void SetPlayer(GameObject PlayerGrid)
    {
        this.initialMove = false;
        this.PlayerGrid = PlayerGrid;
        playerHand = new List<Card>();
        Init();
        switch (playerType)
        {
            case PlayerType.Human:
                // Set up human player
                break;
            case PlayerType.Computer:
                // Set up computer player
                // add computer component
                gameObject.AddComponent<Computer>();
                break;
        }
    }
    public bool IsComputer()
    {
        return playerType == PlayerType.Computer;
    }
    // init board with 14 cards from the rummikub deck
    public void Init()
    {
        uiManager.InitPlayerTileSlots(PlayerGrid.transform);
        // Draw random cards and assign them to 14 slots on the player board
        for (int i = 0; i < 14; i++)
        {
            GameObject tileSlot = PlayerGrid.transform.GetChild(i).gameObject;

            // Draw a random card from the deck using RummikubDeck
            Card randomCard = uiManager.InstinitanteCard(board.GetRummikubDeckInstance().DrawRandomCardFromDeck(), tileSlot);
            playerHand.Add(randomCard);
            if (randomCard == null)
            {
                Debug.LogWarning("Unable to draw a card for the player's board.");
            }

        }


    }

    public void SetBoardVisiblity(bool isActive)
    {
        PlayerGrid.SetActive(isActive);
    }
    //get player grid
    public GameObject GetPlayerGrid()
    {
        return PlayerGrid;
    }

    public int GetEmptySlotIndex()
    {
        for (int i = 0; i < PlayerGrid.transform.childCount; i++)
        {
            GameObject tileSlot = PlayerGrid.transform.GetChild(i).gameObject;

            // Check if the slot is empty
            if (tileSlot.transform.childCount == Constants.EmptyTileSlot)
            {
                // Return the index of the empty slot
                return i;
            }
        }
        // Return -1 if no empty slot is found
        return -1;
    }

    public void AddCardToList(Card card)
    {
        playerHand.Add(card);
    }

    public void RemoveCardFromList(Card card)
    {
        playerHand.Remove(card);
    }

    public bool GetInitialMove()
    {
        return initialMove;
    
    }

    public void SetInitialMove(bool initialMove)
    {
        this.initialMove = initialMove;
    }
 
   public void DrawCardFromDeck()
    {
        // Get the index of the first empty slot
        int emptySlotIndex = GetEmptySlotIndex();
        // Check if an empty slot is found
        if (emptySlotIndex != -1)
        {
            try
            {
                GameObject tileSlot =PlayerGrid.transform.GetChild(emptySlotIndex).gameObject;
                // Draw a random card from the deck using RummikubDeck
                Card randomCard = uiManager.InstinitanteCard(board.GetRummikubDeckInstance().DrawRandomCardFromDeck(), tileSlot);
            }
            catch (EmptyDeckException)
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

    public override string ToString()
    {
        return base.ToString();
    }

    public List<Card> GetPlayerHand()
    {
        return playerHand;
    }

    public object GetPlayerType()
    {
        return playerType;
    }
}
