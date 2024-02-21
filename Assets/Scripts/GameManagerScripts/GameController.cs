using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private List<Player> playersList;
    private int currentTurn;
    [SerializeField] private UImanager uiManager;

    // Start is called before the first frame update
    void Start()
    {
        playersList = new List<Player>();
        currentTurn = 0;
        InitPlayerList();
        playersList[currentTurn].SetBoardVisiblity(true);
    }

    // Function to initialize the playersList
    private void InitPlayerList()
    {
        // Find all GameObjects with the "Player" tag
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        // Iterate through each GameObject
        print("playerObjects.Length: " + playerObjects.Length);
        foreach (GameObject playerObject in playerObjects)
        {
            Player player = playerObject.GetComponent<Player>();
            // If the Player component exists, add it to the playersList
            if (player != null)
            {
                playersList.Add(player);
                GameObject playerGrid = uiManager.InstantiatePlayerGrid();
                player.SetPlayer(playerGrid);
            }
            else
            {
                throw new System.Exception("Player object does not have a Player component!");
            }
        }
    }
    // change the turn to the next player
    private void ChangeTurn()
    {
        playersList[currentTurn].SetBoardVisiblity(false);
        currentTurn = (currentTurn + 1) % playersList.Count;
        playersList[currentTurn].SetBoardVisiblity(true);

    }

    public void Confrim()
    {
        if (isBoardValid())
        {
            ChangeTurn();
        }
    }
    public bool isBoardValid()
    {
        return true;
    }
    public Player GetCurrentPlayer()
    {
        return playersList[currentTurn];
    }

    public int GetCurrentPlayerIndex()
    {
        return currentTurn;
    }
}
