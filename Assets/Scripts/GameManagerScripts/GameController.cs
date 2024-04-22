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
        InitPlayerList();
        GenerateRandomTurn();
    }
    // Function to generate a random turn
    private void GenerateRandomTurn()
    {
        currentTurn = UnityEngine.Random.Range(0, playersList.Count);
        playersList[currentTurn].SetBoardVisiblity(true);
        uiManager.UpdateTurnText();
        CheckIfComputerTurn();

    }
    public void CheckIfComputerTurn()
    {
        if (GetCurrentPlayer().IsComputer())
        {
            Computer computer = GetCurrentPlayer().GetComponent<Computer>();
            StartCoroutine(computer.ComputerMove());
        }

    }
    // Function to initialize the playersList
    private void InitPlayerList()
    {
        // Find all GameObjects with the "Player" tag
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        // Iterate through each GameObject
        foreach (GameObject playerObject in playerObjects)
        {
            Player player = playerObject.GetComponent<Player>();
            // If the Player component exists, add it to the playersList
            if (player != null)
            {
                // start the initialization of the player in case of computer
                if (player.IsComputer())
                {
                    player.gameObject.AddComponent<Computer>().Initialize(player);
                }
                playersList.Add(player);
                GameObject playerGrid = uiManager.InstantiatePlayerGrid();
                player.SetPlayer(playerGrid);}
            else
            {
                throw new System.Exception("Player object does not have a Player component!");
            }
        }
    }
    // change the turn to the next player
    public void ChangeTurn()
    {
        CheckWin();
        playersList[currentTurn].SetBoardVisiblity(false);
        currentTurn = (currentTurn + 1) % playersList.Count;
        playersList[currentTurn].SetBoardVisiblity(true);
        CheckIfComputerTurn();
    }

 
    public Player GetCurrentPlayer()
    {
        return playersList[currentTurn];
    }

    public int GetCurrentPlayerIndex()
    {
        return currentTurn;
    }

    public void CheckWin()
    {   
        if(playersList[GetCurrentPlayerIndex()].IsDeckEmpty())
        {
            //gameController.GetCurrentPlayer().GetPlayerType().ToString() + (gameController.GetCurrentPlayerIndex() + 1
            Debug.Log("******************************************************Player "+GetCurrentPlayer().GetPlayerType().ToString()+(GetCurrentPlayerIndex() + 1)+" wins******************************************************");
            //uiManager.ShowWinScreen(playersList[currentTurn].GetPlayerName());
        }

    }
    // public void CheckWin()
    // {
    //     if (playersList[currentTurn].CheckWin())
    //     {
    //         uiManager.ShowWinScreen(playersList[currentTurn].GetPlayerName());
    //     }
    //     else if (playersList.All(player => player.CheckDraw()))
    //     {
    //         uiManager.ShowDrawScreen();
    //     }
    //     else
    //     {
    //         ChangeTurn();
    //     }
    // }
}
