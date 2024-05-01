using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class GameBoard : MonoBehaviour
{

    // Reference to the rummikub deck, readonly to prevent the deck from being replaced by another deck
    private readonly RummikubDeck rummikubDeck = new RummikubDeck();
    [SerializeField] private UImanager uiManager;

    public Board board;

    // Backup for the undo functionality
    public Board boardBackup;

    // Stack to keep track of the moves on the board for the undo functionality
    // readonly stack to prevent the stack from being replaced
    private readonly Stack<Card> movesStack = new Stack<Card>();
    private GameController gameController;


    // Return instance of rummikub deck
    public RummikubDeck GetRummikubDeckInstance() => rummikubDeck;

    // Start is called before the first frame update
    void Start()
    {
        //get game controller instance
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
        // this class is actually the board grid so we send "transform" 
        uiManager.InitBoardTileSlots(transform);
        //game board valid sets need to have at max 0-7 sets which is the rows on board and every set can have many CardsSet which will held by location on board
        board = new Board();
        boardBackup = new Board();
        ExplainGameRules();

    }
    public void UndoMoves()
    {
        while (movesStack.Count > Constants.EmptyStack)
        {
            Card card = movesStack.Pop();
            UndoMoveForCard(card);
        }
        // Undo logic for the stack of moves
        board = new Board(boardBackup);
        PrintGameBoardValidSets();
    }

    public bool IsExistForStack(Card card) => movesStack.Contains(card);
    // Undo logic for a single card O(1)
    private void UndoMoveForCard(Card card)
    {
        // Undo logic for a single card
        if (!card.CameFromPlayerHand)
        {
            // Move the card back to its original position
            DraggableItem draggableItem = card.GetComponent<DraggableItem>();
            gameController.GetCurrentPlayer().AddCardToList(card);
            if (draggableItem != null)
            {
                Debug.Log("<color=yellow>from board to board undo</color>");
                //card.ParentBeforeDrag is the tile slot location on board when pushed first to the stack
                draggableItem.parentAfterDrag = card.ParentBeforeDrag;
                card.transform.parent = draggableItem.parentAfterDrag;
                card.transform.localPosition = Vector3.zero;
                // update the card posistion to suit the old one
                card.Position = card.OldPositionBeforeDrag;
            }
        }
        else
        {
            // Move the card back to the player's hand
            Debug.Log("<color=yellow>from board to player undo</color>");
            //update the card position to null
            gameController.GetCurrentPlayer().AddCardToList(card);
            // Get the empty slot index in the player's hand and save the card in that slot
            GameObject tileSlot = gameController.GetCurrentPlayer().GetPlayerGrid()
            .transform.GetChild(gameController.GetCurrentPlayer().GetEmptySlotIndex()).gameObject;
            // visual the card on the new postion
            card.transform.SetParent(tileSlot.transform);
            card.transform.localPosition = Vector3.zero;
        }
    }

    // add card to the stack of moves   
    public void AddCardToMovesStack(Card card) => movesStack.Push(card);

    public Stack<Card> GetMovesStack() => movesStack;
    // Move Card from GameBoard to GameBoard

    // Print all items in gameBoardValidSets
    public void PrintGameBoardValidSets()
    {
        // create these two hashes
        Dictionary<SetPosition, CardsSet> gameBoardValidSets = board.GetGameBoardValidSetsTable();
        Dictionary<int, SetPosition> cardToSetPos = board.GetCardsToSetsTable();
        Debug.Log("<color=red>---------------------------------------Print board---------------------------------------</color>");
        //print the keys and their values from gameboard
        foreach (KeyValuePair<SetPosition, CardsSet> entry in gameBoardValidSets)
        {
            Debug.Log("<color=green> Key:" + entry.Key.GetId() + " Value:" + entry.Value.ToString() + "</color>");
        }
        Debug.Log("<color=red>Print keys of Sets</color>");
        foreach (int key in cardToSetPos.Keys)
        {
            Debug.Log("<color=orange> Key:" + key + " Set Pos:" + cardToSetPos[key].GetId() + "</color>");
        }
    }



    // O(n) where n is the number of cards in the set instead of O(n^2) in the previous implementation where n is the number of sets on the board multiply the sum of the cards in all the sets List<Card>[] sets, iteration only on some row
    // Handle the movement of a card from the game board to the game board
    public void MoveCardFromGameBoardToGameBoard(Card card)
    {
        // Remove the card from its current set
        SetPosition oldSetPos = board.FindCardSetPosition(card);
        CardsSet oldSet = board.GetCardsSet(oldSetPos);
        int key = GetKeyFromPosition(card.OldPosition);
        // Remove the card from the set O(n) where n is the number of cards in the set
        int cardIndex = oldSet.RemoveCard(card);
        if (oldSet.set.Count == 0)
        {
            // If the old set is now empty, remove it. Remove the set from the gameBoardValidSets dictionary O(1)
            board.RemoveSetFromBothDic(key);
        }
        // If the card was removed from the middle of the set, split the set into two
        else
        {
            // cardIndex = 0 means the card is the first card in the set, cardIndex = oldSet.set.Count means the card is the last card in the set else the card is in the middle of the set
            if (cardIndex > 0 && cardIndex < oldSet.set.Count)
            {
                board.HandleMiddleSplit(card, oldSet, oldSetPos, cardIndex);
            }
            else
            {
                board.HandleBeginningAndEndKeysUpdate(card, oldSet, oldSetPos, cardIndex);
            }
        }
        // If the old set is now empty, remove it
        // Add the card to its new position
        PutInSet(card);
    }

    public void MoveCardFromPlayerHandToGameBoard(Card card, bool canRemove = true)
    {
        if (canRemove)
        {
            gameController.GetCurrentPlayer().RemoveCardFromList(card);
        }
        PutInSet(card);
    }


    // hash function 
    public int GetKeyFromPosition(CardPosition cardPosition)
    {
        return (cardPosition.Row * 100) + cardPosition.Column;
    }


    // O(1). add a card to the set on the board
    public void PutInSet(Card card)
    {
        // Check if the card has neighbors to combine with
        int key = GetKeyFromPosition(card.Position);
        int rightKey = key + 1;
        int leftKey = key - 1;
        // Check if the card has neighbors to combine with
        if (board.CardKeyExistsInSet(rightKey) && board.CardKeyExistsInSet(leftKey))
        {
            CombineSets(card, rightKey, leftKey);
        }// Check if the card has a neighbor to combine with on the right
        else if (board.CardKeyExistsInSet(rightKey))
        {
            AddCardToBeginningOfSet(card, rightKey);
        }// Check if the card has a neighbor to combine with on the left
        else if (board.CardKeyExistsInSet(leftKey))
        {
            AddCardToEndOfSet(card, leftKey);
        }// Create a new set
        else
        {
            CreateNewSet(card, key);
        }
    }

    private void CombineSets(Card card, int rightKey, int leftKey)
    {
        // Combine two sets gameBoardValidSets[cardToSetPos[leftKey]].set and gameBoardValidSets[cardToSetPos[rightKey].set]
        SetPosition leftSetPos = board.GetSetPosition(leftKey);
        SetPosition rightSetPos = board.GetSetPosition(rightKey);
        //no we can get the sets from the positions
        CardsSet leftSet = board.GetCardsSet(leftSetPos);
        CardsSet rightSet = board.GetCardsSet(rightSetPos);
        // update the Set position of the right set to the left set
        board.UpdateSetPosition(rightSet, leftSetPos);
        // add the card to the end of the left set
        leftSet.AddCardToEnd(card);
        // combine the two sets now leftSet will have all the cards
        leftSet.Combine(leftSet, rightSet);
        // remove the right set from the gameBoardValidSets
        board.RemoveValidSet(rightSetPos);
        // remove the right set from the cardToSetPos
        if (rightKey != GetKeyFromPosition(leftSet.GetLastCard().Position))
        {
            board.RemoveSetPosition(rightKey);
        }
        // remove the left set from the cardToSetPos
        if (leftKey != GetKeyFromPosition(leftSet.GetFirstCard().Position))
        {
            board.RemoveSetPosition(leftKey);
        }
    }

    private void AddCardToBeginningOfSet(Card card, int rightKey)
    {
        // Add card to the beginning of the set
        SetPosition setPos = board.GetSetPosition(rightKey);
        //get teh CardsSet from the set position
        board.GetCardsSet(setPos).AddCardToBeginning(card);
        // if the set has more than 2 cards remove the right key
        if (board.GetCardsSet(setPos).set.Count != 2)
        {
            board.RemoveSetPosition(rightKey);
        }
        // rightKey - 1 = key of the card we inserted somewhere on board
        board.SetSetPosition(rightKey - 1, setPos); //put the new key in the cardToSetPos

    }

    private void AddCardToEndOfSet(Card card, int leftKey)
    {
        // Add card to the end of the set
        SetPosition setPos = board.GetSetPosition(leftKey);
        board.GetCardsSet(setPos).AddCardToEnd(card);
        // if the set has more than 2 cards remove the left key (we have a new one)
        if (board.GetCardsSet(setPos).set.Count != 2)
        {
            board.RemoveSetPosition(leftKey);
        }
        // leftKey + 1 = key of the card we inserted somewhere on board
        board.SetSetPosition(leftKey + 1, setPos); //put the new key in the cardToSetPos
    }

    private void CreateNewSet(Card card, int key)
    {
        board.CreateNewSet(card, key);
    }
    public void ExplainGameRules()
    {
        print("The game is played with two sets of 52 cards and 2 jokers. Each " +
            "player has 14 cards in his hand. The goal of the game is to get rid of all the cards in your hand. " +
            "You can do this by creating sets of cards. There are two types of sets: a group and a run. A group is a set of " +
            "3 or 4 cards with the same number but different colors. A run is a set of 3 or more cards with the same color and" +
            " consecutive numbers. A joker can be used as any card. You can add cards to the sets on the board or create new sets." +
            " You can also move cards inside the boards as long as you are not breaking the rules and keeps all the sets valids.");
    }
    // Get the sum of the moves on the board 
    public int GetMovesStackSum()
    {
        int sum = Constants.EmptyStack;
        foreach (Card card in GetMovesStack())
        {
            if (card.CameFromPlayerHand)
                sum += card.Number;
        }
        return sum;
    }
    public int GetMoveStackCountPlayer()
    {
        int sum = Constants.EmptyStack;
        foreach (Card card in GetMovesStack())
        {
            if (card.CameFromPlayerHand)
                sum++;
        }
        return sum;
    }

    // Return instance of rummikub deck

    // O(n) where n is the number of sets
    public bool IsBoardValid()
    {
        Dictionary<SetPosition, CardsSet> gameBoardValidSets = board.GetGameBoardValidSetsTable();

        bool humanCheck = gameController.GetCurrentPlayer().GetInitialMove(); // if the human has made the initial move
        if (GetMovesStackSum() >= Constants.MinFirstSet || humanCheck)
        {
            foreach (CardsSet cardsSet in gameBoardValidSets.Values)
                if (!IsSetValid(cardsSet)) { return false; }
            UpdateIntialMove(); // update the initial move if needed
            return true; // Board is valid
        }
        //print in red need to drop more than 30 points
        Debug.Log("<color=red>Need to drop more than 30 points</color>");
        return false; // cards sum is less than 30  
    }
    public void UpdateIntialMove()
    {
        // Update the initial move for the player - means he dropped sets with more than 30 points
        if (!gameController.GetCurrentPlayer().GetInitialMove())
            gameController.GetCurrentPlayer().SetInitialMove(true);
    }

    // Check if a set of cards is valid (either run or either group)
    private bool IsSetValid(CardsSet cardsSet)
    {
        if (!cardsSet.IsRun() && !cardsSet.IsGroupOfColors())
        {
            Debug.Log($"Invalid set: {string.Join(", ", cardsSet)}");
            Debug.Log($"IsRun: {cardsSet.IsRun()}, IsGroupOfColors: {cardsSet.IsGroupOfColors()}");
            return false; // Set is invalid
        }
        return true; // Set is valid
    }

    // complexity O(n)

    // Get the index of the first empty slot in the game board that can hold 'amount' cards
    public int GetEmptySlotIndexFromGameBoard(int amount)
    {
        GameObject boardGrid = GameObject.FindGameObjectWithTag("BoardGrid");
        int rowCount = Constants.MaxBoardRows;
        int colCount = Constants.MaxBoardColumns;
        // Array to keep track of the number of consecutive empty slots on the same row
        int[] emptySlotsCount = new int[rowCount];

        // Iterate over the slots in the board grid, which is a 2D grid of slots represented as a 1D array max is: 8*29 = 232
        for (int i = 0; i < boardGrid.transform.childCount; i++)
        {
            GameObject currentSlot = boardGrid.transform.GetChild(i).gameObject;
            // get the current row
            int row = i / colCount;

            // Check if the slot is empty
            if (currentSlot.transform.childCount == Constants.EmptyTileSlot)
            {
                emptySlotsCount[row]++;
                // Check if we have found 'amount' consecutive empty slots on the same row
                if (emptySlotsCount[row] == amount + 1 &&
                 (i - amount) % Constants.MaxBoardColumns == 0) // check if the sequence is not broken by the end of the row
                {
                    return i - amount; // Calculate the index of the first slot in the sequence
                }
                else if (emptySlotsCount[row] == amount + 1)
                {
                    // Calculate the index of the first slot in the sequence
                    return i - amount + 1;
                }
            }
            else
            {
                // Reset the count if the slot is not empty
                emptySlotsCount[row] = 0;
            }
        }

        // Return -1 if no sequence of 'amount' consecutive empty slots on the same row is found
        return -1;
    }

    public void PlayCardSetOnBoard(CardsSet cardsSet)
    {
        int tileslot = GetEmptySlotIndexFromGameBoard(cardsSet.set.Count + 1);
        tileslot++;
        foreach (Card card in cardsSet.set)
        {

            card.OldPosition = card.Position;
            card.Position.SetTileSlot(tileslot);
            uiManager.MoveCardToBoard(card, tileslot, true);
            tileslot++;
            // in case of manual undo keep track of the logic for the computer even tho we allow only valid moves
            AddCardToMovesStack(card);
            // move and remove the card
            MoveCardFromPlayerHandToGameBoard(card);
        }

        gameController.GetCurrentPlayer().PrintCards();

    }

    // Play a card on the board at a specific tile slot and remove it from the player's hand, 
    // assume the play is from the player hand

    // O(1)
    public void PlayCardOnBoard(Card card, int tileslot, bool canRemove = true)
    {
        // assume already check no nehibors to combine my love
        uiManager.MoveCardToBoard(card, tileslot, true);
        AddCardToMovesStack(card);
        MoveCardFromPlayerHandToGameBoard(card, canRemove);
    }
    // Rearrange the cards on the board with the given card to the end or the beginning of the set
    // while keeping the sets valid and the board rules with visual update
    // O(n) where n is the number of cards in the set
    public void RearrangeCardsSet(SetPosition setPosition, Card givenCard, bool addAtTheEnd)
    {
        Dictionary<SetPosition, CardsSet> gameBoardValidSets = board.GetGameBoardValidSetsTable();
        CardsSet set = gameBoardValidSets[setPosition];
        int tileslot = GetEmptySlotIndexFromGameBoard(set.set.Count + 1);
        if (!addAtTheEnd)
        {
            givenCard.OldPosition = givenCard.Position;
            givenCard.Position.SetTileSlot(tileslot);
            tileslot++;
        }
        // move the cards to free location and put the last card at the end or at the beginning based on the boolean
        foreach (Card card in set.set)
        {
            card.OldPosition = card.Position;
            card.Position.SetTileSlot(tileslot);
            // update visualy
            uiManager.MoveCardToBoard(card, tileslot, false);
            tileslot++;
            // in case of manual undo keep track of the logic for the computer even tho we allow only valid moves
            AddCardToMovesStack(card);
            // move and remove the card
        }

        if (addAtTheEnd)
        {
            givenCard.OldPosition = givenCard.Position;
            givenCard.Position.SetTileSlot(tileslot);
        }
        // move the given card to the free location
        uiManager.MoveCardToBoard(givenCard, givenCard.Position.GetTileSlot(), true);
    }
    /// <summary>
    /// Checks if there is space for a card in the specified position on the game board.
    /// </summary>
    /// <param name="isEnd">Indicates whether the card is being placed at the end of the set or at the beginning.</param>
    /// <param name="gameBoard">The game board object.</param>
    /// <returns>True if there is space for a card, false otherwise.</returns>
    // O(1)
    public bool IsSpaceForCard(bool isEnd, CardsSet set)
    {
        if (isEnd)
        {
            return IsSpaceForCardAtEnd(set);
        }
        return IsSpaceForCardAtBeginning(set);
    }

    private bool IsSpaceForCardAtEnd(CardsSet set)
    {
        GameObject secondTileSlot = null;
        int lastCardColumn = set.GetLastCard().Position.Column;
        if (lastCardColumn != Constants.MaxBoardColumns - 1 && lastCardColumn != Constants.MaxBoardColumns - 2)
        {
            int tileSlotIndex = set.GetLastCard().Position.GetTileSlot() + 2;
            if (tileSlotIndex < this.transform.childCount)
            {
                secondTileSlot = this.transform.GetChild(tileSlotIndex).gameObject;
            }
        }
        if (secondTileSlot != null && (secondTileSlot.transform.childCount == Constants.EmptyTileSlot || lastCardColumn == Constants.MaxBoardColumns - 2))
        {
            return true;
        }
        return false;
    }

    private bool IsSpaceForCardAtBeginning(CardsSet set)
    {
        GameObject secondTileSlot = null;
        int firstCardColumn = set.GetFirstCard().Position.Column;
        if (firstCardColumn != 0 && firstCardColumn != 1)
        {
            int tileSlotIndex = set.GetFirstCard().Position.GetTileSlot() - 2;
            if (tileSlotIndex >= 0)
            {
                secondTileSlot = transform.GetChild(tileSlotIndex).gameObject;
            }
        }
        if (secondTileSlot != null && (secondTileSlot.transform.childCount == Constants.EmptyTileSlot || firstCardColumn == 1))
        {
            return true;
        }
        return false;
    }

}