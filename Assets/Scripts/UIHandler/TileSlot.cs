using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileSlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] private GameBoard board;
    [HideInInspector] private UImanager uiManager;
    [HideInInspector] private GameManager gameManager;

    private void Start()
    {
        this.board = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameBoard>();
        this.uiManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UImanager>();
        this.gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    // triggered when a card is dropped on the tile slot 
    public void OnDrop(PointerEventData eventData)
    {
        if (gameManager.GetTurn() != 1 && transform.childCount == 0)
        {
            HandleValidDrop(eventData);

        }
        else
        {
            ShowWaitForYourTurnDialog();
        }
    }

    private void HandleValidDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        Card card = dropped.GetComponent<Card>();
        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
        draggableItem.parentAfterDrag = transform;


        if (card != null)
        {
            if (card.Position != null)
            {
                card.OldPosition = new CardPosition { Row = card.Position.Row, Column = card.Position.Column };
            }
            if (transform.parent.name == "HumanGrid")
            {
                HandleDropOnHumanGrid(card, draggableItem);
            }
            else
            {
                HandleDropOnBoardGrid(card, draggableItem);
            }
        }

        board.PrintGameBoardValidSets();
    }

    private void HandleDropOnHumanGrid(Card card, DraggableItem draggableItem)
    {
        if (draggableItem.parentBeforeDrag.transform.parent.name == "BoardGrid")
        {
            // dont allow moving back from board to human grid
            draggableItem.parentAfterDrag = draggableItem.parentBeforeDrag;
            card.transform.SetParent(draggableItem.parentAfterDrag);
            card.transform.localPosition = Vector3.zero;

        }
        else
        {
            // movement inside human grid
            card.Position = new CardPosition { Row = GetRowIndexHuman(), Column = GetColumnIndexHuman() };
            Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.name + " Dropped at Row: " + card.Position.Row + ", Column: " + card.Position.Column + " HumanGrid " + card.CameFromHumanHand);
        }
    }

    private void HandleDropOnBoardGrid(Card card, DraggableItem draggableItem)
    {
        card.Position = new CardPosition { Row = GetRowIndexBoard(), Column = GetColumnIndexBoard() };
        Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.name + " Dropped at Row: " +card.Position.Row + ", Column: " + card.Position.Column + " BoardGrid " + card.CameFromHumanHand);

        // movement from human hand to board grid
        if (draggableItem.parentBeforeDrag.transform.parent.name == "HumanGrid")
        {
            card.CameFromHumanHand = true;
            // update the position of the card in the game board
            board.MoveCardFromHumanHandToGameBoard(card);
            // push the card to the moves stack
            print("Pushed to Player Stack " + card.ToString());
            board.AddCardToMovesStack(card);
        }
        // movement inside board grid
        else
        {
            // if the card is not in the stack (didn't came from human), push it to the stack as it came from the board
            if (!board.IsExistForStack(card))
            {
                card.CameFromHumanHand = false;
                // save the parent before drag in case of moving multiple times the same card in the board
                card.ParentBeforeDrag = draggableItem.parentBeforeDrag;
                print("Pushed to Board Stack " + card.ToString());
                // push the card to the moves stack
                board.AddCardToMovesStack(card);
            }
            //update the position of the card in the game board
            board.MoveCardFromGameBoardToGameBoard(card);
        }
    }

    private void ShowWaitForYourTurnDialog()
    {
        Debug.Log("Wait For Your Turn");
    }

    private int GetRowIndexHuman()
    {
        int totalColumns = 20;
        return transform.GetSiblingIndex() / totalColumns;
    }

    private int GetColumnIndexHuman()
    {
        int totalColumns = 20;
        return transform.GetSiblingIndex() % totalColumns;
    }

    private int GetRowIndexBoard()
    {
        int totalColumns = 29;
        return transform.GetSiblingIndex() / totalColumns;
    }

    private int GetColumnIndexBoard()
    {
        int totalColumns = 29;
        return transform.GetSiblingIndex() % totalColumns;
    }
}
