using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class RummikubMove : MonoBehaviour
{
    // Reference to GameManager so we can change turns
    [HideInInspector] GameManager gameManager;
    // Reference to the ui manager so we can change the turn display
    [HideInInspector] UImanager uiManager;
    // The delay for the computer move
    public float computerMoveDelay = 0.1f;
    // game board reference
    [HideInInspector] private GameBoard gameBoard;
    // BoardGrid reference 
    [SerializeField] GameObject BoardGrid;
    // create patirial set
    List<PartialCardSet> partialCardSet;
    // free cards
    List<Card> freeCards;
    // computerhand
    List<Card> computerHand;
    private void Start()
    {
        // create a new list of partial card sets
        partialCardSet = new List<PartialCardSet>();
        partialCardSet.Add(new PartialCardSet());   
        computerHand = new List<Card>();
        // create a new list of free cards
        freeCards = new List<Card>();
        // Get the game manager and ui manager from the game manager object
        this.gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        this.uiManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UImanager>();
        this.gameBoard = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameBoard>();
        // Subscribe to the turn changed event
        gameManager.TurnChanged += HandleTurnChanged;
    }



    private void HandleTurnChanged(int newTurn)
    {
        if (newTurn == Constants.AITurn)
        {
            StartCoroutine(ComputerMove()); // start the computer move coroutine
        }
    }
    private IEnumerator ComputerMove()
    {
        yield return new WaitForSeconds(computerMoveDelay); // wait for the delay
        GenerateCards(); // generate the cards for the freeCards and PatirialCardSet
        DrawRandomCard();
        uiManager.ConfirmMove(); // announce the computer move to the player
        gameBoard.PrintHumanHand();
        gameBoard.PrintComputerHand();
    }
    public void GenerateCards()
    {
        computerHand = gameBoard.GetComputerHand();
        PrintComputerHand();

        computerHand.Sort((x, y) => x.Number.CompareTo(y.Number));
        partialCardSet = IntilizePartialCardSet();
        PrintPartialSet();
        PrintFreeCards();
    }
    public void PrintFreeCards()
    {
        // Print free cards in red
        Debug.Log("<color=red>Free Cards:</color>");
        // Use a counter to append a unique identifier to each card message
        int counter = 0;
        foreach (Card card in freeCards)
        {
            // Append the counter to the card message
            Debug.Log($" {counter}: <color=green> {card}</color>");
            counter++;
        }
    }
    
    // print duplicate cards 
  

    public void PrintComputerHand()
    {
        // Print computer hand in green
        Debug.Log("<color=green>Computer Hand: 00000000000000</color>");

        // Use a counter to append a unique identifier to each card message
        int counter = 0;
        foreach (Card card in computerHand)
        {
            // Append the counter to the card message
            Debug.Log($" {counter}: <color=green> {card}</color>");
            counter++;
        }
    }

    public List<PartialCardSet> IntilizePartialCardSet()
    {
        List<Card> removeList = new List<Card>();
        foreach (Card card in computerHand)
        {
            bool addedToExistingSet = false;

            foreach (PartialCardSet set in partialCardSet)
            {
                if (set.CanAddFirstCard())
                {
                    set.AddCardEnd(card);
                    removeList.Add(card);
                    addedToExistingSet = true;
                    print($"<color=blue>{card}</color> added to existing set as first: {set} ");
                    break;
                }
                else if (set.CanAddSecondCard())
                {
                    if (set.TryToAddCard(card))
                    {
                        removeList.Add(card);
                        addedToExistingSet = true;
                        print($"<color=blue>{card}</color> added to existing set as second: {set}");
                        break;
                    }
                }
            }

            if (!addedToExistingSet)
            {
                PartialCardSet newSet = new PartialCardSet(card);
                partialCardSet.Add(newSet);
                removeList.Add(card); // Add the card to the removeList
                print($"<color=blue>{card}</color> added to a new set: {newSet}");
            }
        }

        // Remove cards from the computerHand after they have been added to a PartialCardSet
        foreach (Card card in removeList)
        {
            computerHand.Remove(card);
        }

        return RemoveSingleSets(); // Only return sets with exactly two cards
    }

    public List<PartialCardSet> RemoveSingleSets()
    {
        List<PartialCardSet> newPartialCardSet = new List<PartialCardSet>();
        foreach (PartialCardSet set in partialCardSet)
        {
            if (set.partialSet.Count == Constants.MaxPartialSet)
            {
                newPartialCardSet.Add(set);
            }
            else
            {
                freeCards.Add(set.partialSet[0]);
            }
        }
        return  newPartialCardSet;
    }

    public void DrawRandomCard()
    {
        // Get the index of the first empty slot
        int emptySlotIndex = GetEmptySlotIndex();
        // Check if an empty slot is found
        if (emptySlotIndex != -1)
        {
            try
            {
                GameObject tileSlot = BoardGrid.transform.GetChild(emptySlotIndex).gameObject;
                // Draw a random card from the deck using RummikubDeck
                Card randomCard = uiManager.InstinitanteCard(gameBoard.GetRandomCardFromComputer(), tileSlot);
                randomCard.Position = new CardPosition(emptySlotIndex);

                gameBoard.MoveCardFromComputerHandToGameBoard(randomCard);
            }
            catch (EmptyDeckException)
            {
                Debug.LogWarning("No cards left in the deck.");
            }
        }
        else
        {
            Debug.LogWarning("No empty slots available.");
            // Handle the case where no empty slots are found, perhaps prompt the user or handle it accordingly.
        }
    }
    public int GetEmptySlotIndex()
    {
        for (int i = 0; i < BoardGrid.transform.childCount; i++)
        {
            GameObject firstTileSlot = BoardGrid.transform.GetChild(i).gameObject;
            // Check if the slot is empty
            if (firstTileSlot.transform.childCount == Constants.EmptyTileSlot)
            {
                // Return the index of the empty slot
                return i;
            }
        }
        // Return -1 if no empty slot is found
        return -1;
    }
    //print partial set
    public void PrintPartialSet()
    {
        //print partial set in red use <color> red in debug log
        Debug.Log("<color=blue>Partial Sets:</color>");

        int i = 0;
        foreach (PartialCardSet set in partialCardSet)
        {
            Debug.Log("<color=blue>"+i+"</color>");

            Debug.Log(set+" LOC "+ i);
            i++;
        }
    }
    //print free cards

    private void OnDestroy()
    {
        gameManager.TurnChanged -= HandleTurnChanged; // unsubscribe from the turn changed event
    }
}