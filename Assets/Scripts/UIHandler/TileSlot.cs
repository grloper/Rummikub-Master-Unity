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
            if (!card.CameFromHumanHand)
            {
                draggableItem.parentAfterDrag = draggableItem.parentBeforeDrag;
                card.transform.SetParent(draggableItem.parentAfterDrag);
                card.transform.localPosition = Vector3.zero;
            }
            else
            {
                board.MoveCardFromGameBoardToHumanHand(card);
            }
        }
        else
        {
            // dropped from deck to human grid
            card.Position = new CardPosition { Row = GetRowIndexHuman(), Column = GetColumnIndexHuman() };
            Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.name + " Dropped at Row: " + card.Position.Row + ", Column: " + card.Position.Column + " HumanGrid " + card.CameFromHumanHand);
        }
    }

    private void HandleDropOnBoardGrid(Card card, DraggableItem draggableItem)
    {
        card.Position = new CardPosition { Row = GetRowIndexBoard(), Column = GetColumnIndexBoard() };
        Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.name + " Dropped at Row: " +
            card.Position.Row + ", Column: " + card.Position.Column + " BoardGrid " + card.CameFromHumanHand);

        if (draggableItem.parentBeforeDrag.transform.parent.name == "HumanGrid")
        {
            card.CameFromHumanHand = true;
            print("From human to board");
            board.MoveCardFromHumanHandToGameBoard(card);
            print("Pushed to Player Stack " + card.ToString());
            board.AddCardToMovesStack(card);
        }
        else
        {
            if (!board.IsExistForStack(card))
            {
                card.CameFromHumanHand = false;
                card.ParentBeforeDrag = draggableItem.parentBeforeDrag;
                print("Pushed to Board Stack " + card.ToString());
                board.AddCardToMovesStack(card);
            }
            print("From board to board tile");
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
