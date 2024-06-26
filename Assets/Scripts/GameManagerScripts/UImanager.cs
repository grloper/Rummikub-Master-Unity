using Microsoft.Unity.VisualStudio.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
public class UImanager : MonoBehaviour
{
    public List<Sprite> cardsUI = new List<Sprite>();
    //Game Tiles, TileSlots prefabs
    [SerializeField] private GameObject PrefabTile;
    [SerializeField] private GameObject PrefabTileSlot;
    [SerializeField] private GameObject PrefabTileSlotPlayer;
    [SerializeField] private GameController gameController;
    [SerializeField] private GameObject PrefabPlayerGrid;
    [SerializeField] private GameObject Canvas;
    [SerializeField] private TextMeshProUGUI turnDisplayText;
    [SerializeField] private GameBoard board;
    // Reference to the deck button so we can change the text
    [SerializeField] TextMeshProUGUI btnDeckText;



    //institnate player grid in the canvas
    public GameObject InstantiatePlayerGrid()
    {
        GameObject playerGrid = Instantiate(PrefabPlayerGrid, Canvas.transform);
        //PreserveRectTransformValues(playerGrid.GetComponent<RectTransform>(), PrefabPlayerGrid.GetComponent<RectTransform>());
        playerGrid.SetActive(false);
        return playerGrid;
    }


    private void Start()
    {
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
    }
    public void BtnDeckClick()
    {
        DrawACardFromDeck();
    }
    public void DrawACardFromDeck()
    {
        // if there is a move to undo - undo it
        if (board.GetMovesStack().Count > Constants.EmptyStack)
        {
            print("Undoing");
            Undo();
        } // if the deck is not empty - draw a card and update the turn\deck text
        if (board.GetRummikubDeckInstance().GetDeckLength() > Constants.EmptyDeck)
        {
            gameController.GetCurrentPlayer().DrawCardFromDeck();
            gameController.ChangeTurn();
            UpdateTurnText();

            btnDeckText.text = "Deck:\n" + board.GetRummikubDeckInstance().GetDeckLength();
        }
        else
        { // if the deck is empty - update the deck text
            btnDeckText.text = "Deck:\nEmpty";
        }
    }
    public void Undo()
    {
        // If the stack is empty, print an error
        if (board.GetMovesStack().Count == Constants.EmptyStack)
            throw new EmptyDeckException();
        else
            board.UndoMoves();
    }
    public void BtnUndoClick()
    {
        try
        {
            Undo();
        }
        catch (EmptyDeckException)
        {
            print("No moves to undo");
        }
    }
    public Card InstinitanteCard(Card GivvenCard, GameObject tileslot)
    {
        Card card = InstinitanteCard(GivvenCard);
        card.transform.SetParent(tileslot.transform);
        return card;
    }
    public Card InstinitanteCard(Card GivvenCard)
    {
        GameObject card = Instantiate(PrefabTile);
        // Set the card's sprite to the correct sprite
        card.GetComponent<Image>().sprite = cardsUI[CalculateIndexOfSprite(GivvenCard)];
        // Change the variable from GameObject to Card 
        Card newCard = card.GetComponent<Card>();
        // Set the card's color and number
        newCard.Color = GivvenCard.Color;
        newCard.Number = GivvenCard.Number;
        newCard.Position = GivvenCard.Position;
        return newCard;
    }
    private int CalculateIndexOfSprite(Card card)
    {
        if (card.Number == Constants.JokerRank)
        {
            return (card.Number - 2) * Constants.MaxSuit + (int)card.Color; //handler for jokers (assumes jokers = 14) to save memory
        }
        return (card.Number - 1) * Constants.MaxSuit + (int)card.Color;  // algorithm thats gets the location of the card in the sprite list
    }

    // Initialize Player Board with 40 slots 2HEIGHT*20WIDTH
    public void InitPlayerTileSlots(Transform playerGrid)
    {
        // Initialize Player Board with 44 slots
        for (int i = 0; i < Constants.MaxPlayerBoardSlots; i++)
        {
            Instantiate(PrefabTileSlotPlayer, playerGrid.transform);
        }

    }
    // Initialize Game Board with 232 slots 8HEIGHT*29WIDTH
    public void InitBoardTileSlots(Transform boardGrid)
    {
        // Initialize Game Board
        for (int i = 0; i < Constants.MaxGameBoardSlots; i++)
        {
            Instantiate(PrefabTileSlot, boardGrid.transform);
        }

    }
    public void BtnConfirmMoveClick()
    {
        ConfirmMove();
    }
    public void ConfirmMove()
    {
        if (board.GetMovesStack().Count == Constants.EmptyStack)//check if dropped cards are valid)
        {
            print("You Did not dropped any cards, tip: draw a card to skip this turn");
        }
        else
        {
            if (board.IsBoardValid())
            {
                // If the board is valid, change the turn and clear the moves stack
                gameController.ChangeTurn();
                UpdateTurnText();
                board.GetMovesStack().Clear();
            } // we have handler to print error when not valid
        }
    }


    public void UpdateTurnText() =>
           turnDisplayText.text = "Turn: " + gameController.GetCurrentPlayer().GetPlayerType().ToString() + (gameController.GetCurrentPlayerIndex() + 1
);





    public void BtnSortByRun()
    {
        // Sort the cards by run
        SortPlayerGrid((card1, card2) =>
        {
            if (card1.Color == card2.Color)
                return card1.Number.CompareTo(card2.Number);
            else
                return card1.Color.CompareTo(card2.Color);
        });
    }

    public void BtnSortByGroup()
    {
        // Sort the cards by group
        SortPlayerGrid((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Color.CompareTo(card2.Color);
            else
                return card1.Number.CompareTo(card2.Number);
        });
    }

    //Function to sort the Player grid by a given comparison
    //private void SortPlayerGrid(Comparison<Card> comparison)
    //{
    //    GameObject playerGrid = gameController.GetCurrentPlayer().GetPlayerGrid();
    //    // Get all the tile slots
    //    List<Transform> tileSlots = new List<Transform>();
    //    foreach (Transform child in playerGrid.transform)
    //    {
    //        tileSlots.Add(child);
    //    }
    //    // Sort the cards in the tile slots
    //    List<Card> cardsToSort = new List<Card>();
    //    foreach (Transform tileSlot in tileSlots)
    //    {
    //        if (tileSlot.childCount > Constants.EmptyTileSlot)
    //        {
    //            Card card = tileSlot.GetChild(0).GetComponent<Card>();
    //            cardsToSort.Add(card);
    //            Destroy(tileSlot.GetChild(0).gameObject);
    //        }
    //    }
    //    // Sort the cards
    //    cardsToSort.Sort(comparison);
    //    // Instantiate the cards in the tile slots
    //    for (int i = 0; i < cardsToSort.Count; i++)
    //    {
    //        GameObject cardObject = Instantiate(PrefabTile);
    //        cardObject.transform.SetParent(tileSlots[i]);
    //        cardObject.transform.localPosition = Vector3.zero;
    //        // Set the card's color and number
    //        Card newCard = cardObject.GetComponent<Card>();
    //        newCard.Color = cardsToSort[i].Color;
    //        newCard.Number = cardsToSort[i].Number;
    //        // Set the card's sprite
    //        int index = CalculateIndexOfSprite(newCard);
    //        cardObject.GetComponent<Image>().sprite = cardsUI[index];
    //    }
    //}

    private void SortPlayerGrid(Comparison<Card> comparison)
    {
        GameObject playerGrid = gameController.GetCurrentPlayer().GetPlayerGrid();

        // Get all the tile slots and existing cards
        List<Transform> tileSlots = new List<Transform>();
        List<Card> cardsToSort = new List<Card>();
        foreach (Transform tileSlot in playerGrid.transform)
        {
            tileSlots.Add(tileSlot);
            if (tileSlot.childCount > Constants.EmptyTileSlot)
            {
                Card card = tileSlot.GetChild(0).GetComponent<Card>();
                cardsToSort.Add(card);
            }
        }

        // Sort the cards
        cardsToSort.Sort(comparison);

        // Rearrange the cards within existing tile slots
        for (int i = 0; i < cardsToSort.Count; i++)
        {
            Card card = cardsToSort[i];
            Transform targetSlot = tileSlots[i]; // Get the target tile slot

            // Check if the card needs to be moved
            if (card.transform.parent != targetSlot)
            {
                card.transform.SetParent(targetSlot); // Move the card to the target slot
            }
        }

        // Update card sprites (optional)
        for (int i = 0; i < cardsToSort.Count; i++)
        {
            Card card = cardsToSort[i];
            int index = CalculateIndexOfSprite(card);
            card.GetComponent<Image>().sprite = cardsUI[index];
        }
    }


    public void MoveCardToBoard(Card card, int tileslot)
    {
        // get the tileslot location from board
        GameObject tileSlot = this.board.transform.GetChild(tileslot).gameObject;
        //update value for proper undo function
        card.CameFromPlayerHand = true;
        card.ParentBeforeDrag = card.transform.parent;
        // visual the card on the new postion
        card.transform.SetParent(tileSlot.transform);
    }
}

