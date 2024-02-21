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

    // act as a constructor for the player because we are using : MonoBehaviour
    public void SetPlayer(GameObject PlayerGrid)
    {
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
                break;
        }
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
}
