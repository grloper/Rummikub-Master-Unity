using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This class is used to manage the game logic, such as the players, the current turn, and the win condition.
public class GameController : MonoBehaviour
{
    private List<Player> playersList; // List of active players
    private int currentTurn; // Index of the current turn
    [SerializeField] private UImanager uiManager; // Reference to the UI manager

    // Start is called before the first frame update
    void Start()
    {
        playersList = new List<Player>(); // Initialize the playersList
        InitPlayerList(); // Initialize the playersList
        GenerateRandomTurn(); // Generate a random turn
    }
    // Function to generate a random turn
    private void GenerateRandomTurn()
    {
        currentTurn = UnityEngine.Random.Range(0, playersList.Count); // Generate a random turn between 0 and the number of players
        playersList[currentTurn].SetBoardVisiblity(true); // Set the board visibility of the current player to true
        uiManager.UpdateTurnText(); // Update the turn text
        CheckIfComputerTurn(); // Check if it is the computer's turn, and if so, make a move automatically
    }
    // Function to check if it is the computer's turn
    public void CheckIfComputerTurn()
    {
        if (GetCurrentPlayer().IsComputer()) // If the current player is a computer
        {
            Computer computer = GetCurrentPlayer().GetComponent<Computer>(); // Get the Computer component
            StartCoroutine(computer.ComputerMove()); // Start the computer's move
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
                player.SetPlayer(playerGrid);
            }
            else
            {
                throw new System.Exception("Player object does not have a Player component!");
            }
        }
    }
    // change the turn to the next player
    public void ChangeTurn()
    {
        CheckWin(); // Check if the current player has won
        playersList[currentTurn].SetBoardVisiblity(false); // Set the board visibility of the current player to false
        currentTurn = (currentTurn + 1) % playersList.Count; // Increment the current turn
        playersList[currentTurn].SetBoardVisiblity(true); // Set the board visibility of the new current player to true
        CheckIfComputerTurn(); // Check if it is the computer's turn
    }


    // function to get the current player
    public Player GetCurrentPlayer()
    {
        return playersList[currentTurn];
    }
    // function to get the current player index
    public int GetCurrentPlayerIndex()
    {
        return currentTurn;
    }

    // a function to check if the current player has won
    public void CheckWin()
    {
        if (playersList[GetCurrentPlayerIndex()].IsDeckEmpty())
        {
            throw new WinException("Player " + GetCurrentPlayer().GetPlayerType().ToString() + (GetCurrentPlayerIndex() + 1) + " wins");
        }
    }
}
