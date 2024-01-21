using Microsoft.Unity.VisualStudio.Editor;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
public class UImanager : MonoBehaviour
{

    // refeence to the HumanGrid
    [SerializeField] GameObject HumanGrid;
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
    [SerializeField] private GameBoard board;
    // Stack of cards that were moved so we can undo the move
    private Stack<GameObject> cardStack = new Stack<GameObject>();

    public void AddCardToStack(GameObject card)
    {
        cardStack.Push(card);
    }


void Start()
    {
        turnDisplayText.text = "Turn: Player";

    }
    public Card InstinitanteCard(Card GivvenCard, GameObject tileslot)
    {
        GameObject card = Instantiate(PrefabTile);
        int index = (GivvenCard.Number - 1) * 4 + (int)GivvenCard.Color; // jokers is when number = 14
        card.GetComponent<Image>().sprite = cardsUI[index];
        card.transform.parent = tileslot.transform;
        return card.GetComponent<Card>();
    }
    public Card InstinitanteCard(Card GivvenCard)
    {
        GameObject card = Instantiate(PrefabTile);
        int index = (GivvenCard.Number - 1) * 4 + (int)GivvenCard.Color; // jokers is when number = 14
        card.GetComponent<Image>().sprite = cardsUI[index];
        return card.GetComponent<Card>();
    }
    public void BtnDeckClick()
    {
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
    public void BtnUndoClick()
    {
        if (gameManager.GetTurn() == 0)
        {
            if (cardStack.Count==0)
            {
                print("No more cards to undo");
            }
            while (cardStack.Count > 0)
            {
                GameObject card = cardStack.Pop();
                board.MoveCardFromGameBoardToHumanHand(card.GetComponent<Card>());
                // Move the card back to its original position
                //DraggableItem draggableItem = card.GetComponent<DraggableItem>();
                //if (draggableItem != null)
                //{
                //    draggableItem.parentAfterDrag = draggableItem.parentBeforeDrag;
                //    card.transform.SetParent(draggableItem.parentAfterDrag);
                //    card.transform.localPosition = Vector3.zero;
                //}
                // move the card to the first empty slot
                int emptySlotIndex = human.GetEmptySlotIndex();
                GameObject tileSlot = HumanGrid.transform.GetChild(emptySlotIndex).gameObject;
                card.transform.SetParent(tileSlot.transform);
                card.transform.localPosition = Vector3.zero;
            }

        }

    }
    public void BtnConfirmMoveClick()
    {
        print("Confirm Move");
        if (gameManager.GetTurn() == 0)
        {
            gameManager.ChangeTurn();
            turnDisplayText.text = "Turn: Computer";
            cardStack.Clear();
        }
        else
        {
            gameManager.ChangeTurn();
            turnDisplayText.text = "Turn: Human";
            cardStack.Clear();

        }
    }
}
