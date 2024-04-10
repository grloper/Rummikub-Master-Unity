using UnityEngine;
using UnityEngine.EventSystems;

public class TileSlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] private GameBoard board;
    [HideInInspector] private GameController gameManager;

    private void Start()
    {
        // Get the game board, UI manager and game manager from the game manager object
        this.board = GameObject.FindGameObjectWithTag("BoardGrid").GetComponent<GameBoard>();
        this.gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
    }

    // triggered when a card is dropped on the tile slot 
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == Constants.EmptyTileSlot)
        {
            HandleValidDrop(eventData);
        }
        else
        {
            ShowWaitForYourTurnDialog();
        }
        board.PrintGameBoardValidSets();
    }
    // Handle the valid drop of a card on the tile slot
    private void HandleValidDrop(PointerEventData eventData)
    {
        // get the card that is being dropped
        GameObject dropped = eventData.pointerDrag;
        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
        draggableItem.parentAfterDrag = transform;


        if (dropped.GetComponent<Card>() != null)
        {
            // save the old position of the card in case of moving multiple times the same card in the board
            if (dropped.GetComponent<Card>().Position != null)
            {
                dropped.GetComponent<Card>().OldPosition = new CardPosition(dropped.GetComponent<Card>().Position.Row, dropped.GetComponent<Card>().Position.Column);
            }
            // if the card is dropped on the human grid
            if (transform.parent.tag == "PlayerGrid")
            {
                HandleDropOnHumanGrid(dropped.GetComponent<Card>(), draggableItem);
            }
            else
            {// if the card is dropped on the board grid
                HandleDropOnBoardGrid(dropped.GetComponent<Card>(), draggableItem);
            }
        }

    }

    private void HandleDropOnHumanGrid(Card card, DraggableItem draggableItem)
    {
        if (draggableItem.parentBeforeDrag.transform.parent.tag == "BoardGrid")
        {
            // dont allow moving back from board to human grid
            draggableItem.parentAfterDrag = draggableItem.parentBeforeDrag;
            card.transform.parent = draggableItem.parentAfterDrag;
            card.transform.localPosition = Vector3.zero;

        }
        else
        {
            // movement inside human grid
            card.Position = new CardPosition(GetRowIndexHuman(), GetColumnIndexHuman());
             Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.tag + " Dropped at Row: " + card.Position.Row + ", Column: " + card.Position.Column + " HumanGrid ,Came from human hand?" + card.CameFromPlayerHand);
        }
    }

    private void HandleDropOnBoardGrid(Card card, DraggableItem draggableItem)
    {
        card.Position = new CardPosition(GetRowIndexBoard(), GetColumnIndexBoard());
        Debug.Log("Dropped from: " + draggableItem.parentBeforeDrag.transform.parent.tag + " Dropped at Row: " +card.Position.Row + ", Column: " + card.Position.Column + " BoardGrid ,Came from human hand?" + card.CameFromPlayerHand);

        // movement from human hand to board grid
        if (draggableItem.parentBeforeDrag.transform.parent.tag == "PlayerGrid")
        {
            card.CameFromPlayerHand = true;
            // update the position of the card in the game board
            board.MoveCardFromPlayerHandToGameBoard(card);
            // push the card to the moves stack
            // print("Pushed to Player Stack " + card.ToString());
            board.AddCardToMovesStack(card);
        }
        // movement inside board grid
        else
        {
            // if the card is not in the stack (didn't came from human), push it to the stack as it came from the board
            if (!board.IsExistForStack(card))
            {
                card.CameFromPlayerHand = false;
                // save the parent before drag in case of moving multiple times the same card in the board
                card.ParentBeforeDrag = draggableItem.parentBeforeDrag;
                //  print("Pushed to Board Stack " + card.ToString());
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


    // unity looks at indexed of tiles slot as an array of 232 items, so we need to calculate the row and column
    // once for the humanGrid and once for the boardGrid
    private int GetRowIndexHuman() => transform.GetSiblingIndex() / Constants.MaxPlayerColumns;
    private int GetColumnIndexHuman() => transform.GetSiblingIndex() % Constants.MaxPlayerColumns;
    private int GetRowIndexBoard() => transform.GetSiblingIndex() / Constants.MaxBoardColumns;
    private int GetColumnIndexBoard() => transform.GetSiblingIndex() % Constants.MaxBoardColumns;

}
