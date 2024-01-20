using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileSlot : MonoBehaviour, IDropHandler
{
     private GameBoard board;

    private void Start()
    {
        this.board= GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameBoard>();
    }
    public void OnDrop(PointerEventData eventData)
    {
        
        if (transform.childCount == 0)
        {
            // Get the card that is being dropped
            GameObject dropped = eventData.pointerDrag;
            // Set the parent of the dropped card to the tile slot
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            // Set the parent of the card to the slot
            draggableItem.parentAfterDrag = transform;
            // Set the position of the dropped card
            Card card = dropped.GetComponent<Card>();
            if (card != null)
            {
                if (transform.parent.name == "HumanGrid")
                {
                    // create a new CardPosition object and assign it to the card
                    card.Position = new CardPosition { Row = GetRowIndexHuman(), Column = GetColumnIndexHuman() };
                    Debug.Log("Dropped from: "+ draggableItem.parentBeforeDrag.transform.parent.name+" Dropped at Row: " + card.Position.Row + ", Column: " + card.Position.Column + " HumanGrid");
                    if (draggableItem.parentBeforeDrag.transform.parent.name=="BoardGrid")
                    {
                        board.MoveCardFromGameBoardToHumanHand(card);
                    }
                    
                }
                //transform.parent.name == "BoardGrid"
                else
                {
                    // create a new CardPosition object and assign it to the card
                    card.Position = new CardPosition { Row = GetRowIndexBoard(), Column = GetColumnIndexBoard() };
                    Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.name + " Dropped at Row: " + card.Position.Row + ", Column: " + card.Position.Column + " BoardGrid");
                    if (draggableItem.parentBeforeDrag.transform.parent.name == "HumanGrid")
                    {
                        board.MoveCardFromHumanHandToGameBoard(card);
                    }
                }
            }
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
