using Microsoft.Unity.VisualStudio.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
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
        if (gameManager.GetTurn() == 0)

        {
            if (board.GetMovesStack().Count == 0)
            {
                print("No more cards to undo");
                return;
            }
            else
            {
                board.UndoMoves();
            }

        }
    }
    public void BtnUndoClick()
    {
        Undo();
    }
    void Start()
    {
        turnDisplayText.text = "Turn: Player";

    }
    public Card InstinitanteCard(Card GivvenCard, GameObject tileslot)
    {
        Card card = InstinitanteCard(GivvenCard);
        card.transform.parent = tileslot.transform;
        return card;
    }
    public Card InstinitanteCard(Card GivvenCard)
    {
        GameObject card = Instantiate(PrefabTile);
        int index = CalculateIndexOfSprite(GivvenCard);
        card.GetComponent<Image>().sprite = cardsUI[index];
        Card newCard = card.GetComponent<Card>();
        newCard.Color = GivvenCard.Color;
        newCard.Number = GivvenCard.Number;

        return newCard;
    }
    private int CalculateIndexOfSprite(Card card)
    {
        // we use this function in order to save memory and use less ui elements
        if (card.Number==15)
        {
            return (card.Number - 2) * 4 + (int)card.Color; // jokers is when number = 15 
        }
        return (card.Number - 1) * 4 + (int)card.Color; 
    }
    public void BtnDeckClick()
    {
        if (!board.IsBoardValid())
        {
        
            print("Invalid Move Undoing");
            Undo();
        }

        if (board.GetRummikubDeckInstance().GetDeckLength() != 0)
        {
            if (gameManager.GetTurn() == 0)
            {
                human.DrawCard();
                gameManager.ChangeTurn();
                turnDisplayText.text = "Turn: Computer";
            }
            else
            {
                computer.DrawCard();
                gameManager.ChangeTurn();
                turnDisplayText.text = "Turn: Human";

            }
            btnDeckText.text = "Deck:\n" + board.GetRummikubDeckInstance().GetDeckLength();
        }
        else
        {
            btnDeckText.text = "Deck:\nEmpty";
        }

    }
    public void BtnConfirmMoveClick()
    {
            //print in yellow confrim move
            Debug.Log("<color=blue>Confirm Move</color>");
        

        if (board.IsBoardValid())
        {

            if (gameManager.GetTurn() == 0)
            {
                board.GetMovesStack().Clear();
                gameManager.ChangeTurn();
                turnDisplayText.text = "Turn: Computer";
            }
            else
            {
                board.GetMovesStack().Clear();
                gameManager.ChangeTurn();
                turnDisplayText.text = "Turn: Human";
            }
        }
        else
        {
            print("Invalid Move");
        }

    }
    public void BtnSortByRun()
    {
       
        // Sort the cards by run
        SortHumanGrid((card1, card2) =>
        {
            // Sort by number first, then by color
            if (card1.Color == card2.Color)
            {
                return card1.Number.CompareTo(card2.Number);
            }
            else
            {
                return card1.Color.CompareTo(card2.Color);
            }
        });
    }

    public void BtnSortByGroup()
    {
        // Sort the cards by group
        
        SortHumanGrid((card1, card2) =>
        {
            // Sort by color first, then by number
            if (card1.Number == card2.Number)
            {
                return card1.Color.CompareTo(card2.Color);
            }
            else
            {
                return card1.Number.CompareTo(card2.Number);
            }
        });
}

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
            if (tileSlot.childCount > 0)
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
