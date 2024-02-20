using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;
public class UImanager : MonoBehaviour
{


    // List of sprites for the cards UI so we can assign them to the cards in the inspector
    public List<Sprite> cardsUI = new List<Sprite>();
    // Reference to the card prefab so we can instantiate it
    [SerializeField] GameObject PrefabTile;
    // Reference to the deck button so we can change the text
    [SerializeField] TextMeshProUGUI btnDeckText;
    // Reference to the turn text so we can change it
    [SerializeField] TextMeshProUGUI turnDisplayText;
    // Reference to Human so we can draw cards
    [SerializeField] Human human;
    // Reference to Computer so we can draw cards
    [SerializeField] Computer computer;
    // Reference to GameManager so we can change turns
    [SerializeField] GameManager gameManager;
    // Reference to the HumanGrid
    [SerializeField] GameObject HumanGrid;
    [SerializeField] private GameBoard board;


    public void Undo()
    {   
        // Undo all moves for the human
        if (gameManager.GetTurn() == Constants.HumanTurn)
        {
            // If the stack is empty, print an error
            if (board.GetMovesStack().Count == Constants.EmptyStack)
               throw new EmptyDeckException();
            else                
                board.UndoMoves();
        }
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
    void Start()
    {
        // Set the initial text for the turn display 
        turnDisplayText.text = "Turn: Player";
    }
    // Function to instantiate a card - the connection between the Card.cs code to the visible Card on screen
    public Card InstinitanteCard(Card GivvenCard, GameObject tileslot)
    {
        Card card = InstinitanteCard(GivvenCard);
        card.transform.SetParent( tileslot.transform);
        return card;
    }
    // Sub function to instantiate a card
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
    // Function to calculate the index of the sprite in the list of spritesUI
    private int CalculateIndexOfSprite(Card card)
    {
        if (card.Number==Constants.JokerRank)
        {
            return (card.Number - 2) * Constants.MaxSuit + (int)card.Color; //handler for jokers (assumes jokers = 14) to save memory
        }
        return (card.Number - 1) * Constants.MaxSuit + (int)card.Color;  // algorithm thats gets the location of the card in the sprite list
    }
    // Function to draw a card from the deck
    public void BtnDeckClick()
    {
        if (gameManager.GetTurn()==Constants.HumanTurn)
            DrawACardFromDeck();
    }
    public void UpdateTurnText() => //update to the current turn 
        turnDisplayText.text = gameManager.GetTurn() == Constants.HumanTurn ? "Turn: Human" : "Turn: Computer";

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
            if (gameManager.GetTurn() == Constants.HumanTurn)
                human.DrawCard();
            else
                computer.DrawCard();
            gameManager.ChangeTurn();
            UpdateTurnText();

            btnDeckText.text = "Deck:\n" + board.GetRummikubDeckInstance().GetDeckLength();
        }
        else
        { // if the deck is empty - update the deck text
            btnDeckText.text = "Deck:\nEmpty";
        }
    }
    

    // Function to confirm the move of the player
    public void BtnConfirmMoveClick()
    {
        if (gameManager.GetTurn()==Constants.HumanTurn)
            ConfirmMove();
    }
    public void ConfirmMove()
    {
        //print confirm in blue use debug log and <color blue
        Debug.Log("<color=blue>Confirm</color>");
        if (board.IsBoardValid())
        {
            // If the board is valid, change the turn and clear the moves stack
            gameManager.ChangeTurn();
            UpdateTurnText();
            board.GetMovesStack().Clear();
        } // we have handler to print error when not valid
    }
  
    public void BtnSortByRun()
    {
        // Sort the cards by run
        SortHumanGrid((card1, card2) =>
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
        SortHumanGrid((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Color.CompareTo(card2.Color);
            else
                return card1.Number.CompareTo(card2.Number);
        });
}

    // Function to sort the human grid by a given comparison 
    private void SortHumanGrid(Comparison<Card> comparison)
    {
        // Get all the tile slots
        List<Transform> tileSlots = new List<Transform>();
        foreach (Transform child in HumanGrid.transform)
        {
            tileSlots.Add(child);
        }
        // Sort the cards in the tile slots
        List<Card> cardsToSort = new List<Card>();
        foreach (Transform tileSlot in tileSlots)
        {
            if (tileSlot.childCount > Constants.EmptyTileSlot)
            {
                Card card = tileSlot.GetChild(0).GetComponent<Card>();
                cardsToSort.Add(card);
                Destroy(tileSlot.GetChild(0).gameObject);
            }
        }
        // Sort the cards
        cardsToSort.Sort(comparison);
        // Instantiate the cards in the tile slots
        for (int i = 0; i < cardsToSort.Count; i++)
        {
            GameObject cardObject = Instantiate(PrefabTile);
            cardObject.transform.SetParent(tileSlots[i]);
            cardObject.transform.localPosition = Vector3.zero;
            // Set the card's color and number
            Card newCard = cardObject.GetComponent<Card>();
            newCard.Color = cardsToSort[i].Color;
            newCard.Number = cardsToSort[i].Number;
            // Set the card's sprite
            int index = CalculateIndexOfSprite(newCard);
            cardObject.GetComponent<Image>().sprite = cardsUI[index];
        }
    }

}
