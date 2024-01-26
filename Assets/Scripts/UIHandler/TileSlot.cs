using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileSlot : MonoBehaviour, IDropHandler
{
    [HideInInspector]  private GameBoard board;
    [HideInInspector] private UImanager uiManager;
    [HideInInspector] private GameManager gameManager;
    private void Start()
    {
        this.board= GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameBoard>();
        this.uiManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UImanager>();
        this.gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

    }
    public void OnDrop(PointerEventData eventData)
    {
        if (gameManager.GetTurn() != 1 && transform.childCount == 0)
        {
            // Get the card that is being dropped
            GameObject dropped = eventData.pointerDrag;
            Card card = dropped.GetComponent<Card>();
            // Set the parent of the dropped card to the tile slot
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            // Set the parent of the card to the slot
            draggableItem.parentAfterDrag = transform;
            // Set the position of the dropped card
            if (card != null)
            {
                if (transform.parent.name == "HumanGrid")
                {
                    if (draggableItem.parentBeforeDrag.transform.parent.name == "BoardGrid")
                    {
                        if (!card.CameFromHumanHand)
                        {
                            // Move the card back to its original position
                            draggableItem.parentAfterDrag = draggableItem.parentBeforeDrag;
                            dropped.transform.SetParent(draggableItem.parentAfterDrag);
                            dropped.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            board.MoveCardFromGameBoardToHumanHand(card);
                        }
                    }
                    else
                    {
                        // create a new CardPosition object and assign it to the card
                        card.Position = new CardPosition { Row = GetRowIndexHuman(), Column = GetColumnIndexHuman() };  
                        Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.name + " Dropped at Row: " + card.Position.Row + ", Column: " + card.Position.Column + " HumanGrid " + card.CameFromHumanHand);
                    }
                }
                else
                {
                    // create a new CardPosition object and assign it to the card
                    card.Position = new CardPosition { Row = GetRowIndexBoard(), Column = GetColumnIndexBoard() };
                    Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.name + " Dropped at Row: " + 
                    card.Position.Row + ", Column: " + card.Position.Column + " BoardGrid "+ card.CameFromHumanHand);
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
                        if (!board.IsExistForStack(card)) // if the card is not in the stack
                        {
                            card.CameFromHumanHand = false;
                            // parentBeforeDrag is saved in case the same card is moved more than once in the board (without this check the undo button will make it go to the last drag pos instead of the first)
                            card.ParentBeforeDrag = draggableItem.parentBeforeDrag;

                            print("Pushed to Board Stack " + card.ToString());
                            board.AddCardToMovesStack(card);

                        }
                        print("From board to board tile");
                        board.MoveCardFromGameBoardToGameBoard(card);
                    }
                }
            }
            board.PrintGameBoardValidSets();
        }
        else
        {
            // Show dialog box: "Wait For Your Turn"
            Debug.Log("Wait For Your Turn");
        }
    }
    private int GetRowIndexHuman()
    {
        //transform.GetSiblingIndex() = the number from 0 to 39 from left to right going upwards
        int totalColumns = 20;
        return transform.GetSiblingIndex() / totalColumns;
    }

    private int GetColumnIndexHuman()
    {
        //transform.GetSiblingIndex() = the number from 0 to 39 from left to right going upwards 
        int totalColumns = 20;
        return transform.GetSiblingIndex() % totalColumns;
    }
    private int GetRowIndexBoard()
    {
        //transform.GetSiblingIndex() = the number from 0 to 231 from left to right going upwards
        int totalColumns = 29;
        return transform.GetSiblingIndex() / totalColumns;
    }

    private int GetColumnIndexBoard()
    {
        int totalColumns = 29;
        //transform.GetSiblingIndex() = the number from 0 to 231 from left to right going upwards
        return transform.GetSiblingIndex() % totalColumns; 
    }




}
