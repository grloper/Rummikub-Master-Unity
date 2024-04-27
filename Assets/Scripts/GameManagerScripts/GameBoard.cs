using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class GameBoard : MonoBehaviour
{
    private RummikubDeck rummikubDeck = new RummikubDeck();
    [SerializeField] private UImanager uiManager;
    //private List<CardsSet>[] gameBoardValidSets = new List<CardsSet>[Constants.MaxBoardRows];

    // <int = CardPosition.Row *100+CardPostition.Column - represent the start of an existing set
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



    // Handle the movement of a card from the game board to the game board
    public async Task MoveCardFromGameBoardToGameBoard(Card card)
    {

        // Remove the card from its current set
        SetPosition oldSetPos = board.FindCardSetPosition(card);
        CardsSet oldSet = board.GetCardsSet(oldSetPos);
        int key = GetKeyFromPosition(card.OldPosition);
        // Remove the card from the set O(n) where n is the number of cards in the set
        int cardIndex = oldSet.RemoveCard(card);
        if (oldSet.set.Count == 0)
        {
            // If the old set is now empty, remove it
            // Remove the set from the gameBoardValidSets dictionary
            // O(1)
            board.RemoveSetFromBothDic(key);
        }
        // If the card was removed from the middle of the set, split the set into two
        else
        {
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
        await PutInSet(card);
    }

    public async Task MoveCardFromPlayerHandToGameBoard(Card card, bool canRemove = true)
    {
        if (canRemove)
        {
            gameController.GetCurrentPlayer().RemoveCardFromList(card);
        }
        await PutInSet(card);
    }


    public async Task MoveCardFromGameBoardToPlayerHand(Card card)
    {
        Dictionary<SetPosition, CardsSet> gameBoardValidSets = board.GetGameBoardValidSetsTable();
        Dictionary<int, SetPosition> cardToSetPos = board.GetCardsToSetsTable();
        // Remove the card from its current set
        SetPosition oldSetPos = new SetPosition(-1);
        CardsSet oldSet = new CardsSet();
        int row = card.OldPosition.Row;
        int col = card.OldPosition.Column;
        int key = GetKeyFromPosition(card.OldPosition);
        if (!cardToSetPos.ContainsKey(key))
        {
            bool found = false;
            // Move left
            //O(n) whee n is the number of cards in the set 
            for (int i = col; i >= 0 && !found; i--)
            {
                key = row * 100 + i;
                if (cardToSetPos.ContainsKey(key))
                {
                    oldSetPos = cardToSetPos[key];
                    oldSet = gameBoardValidSets[oldSetPos];
                    found = true;
                }
            }
        }
        else
        {
            //O(1)
            oldSetPos = cardToSetPos[key];
            oldSet = gameBoardValidSets[oldSetPos];
        }

        // Remove the card from the set O(n) where n is the number of cards in the set
        int cardIndex = oldSet.RemoveCard(card);
        if (oldSet.set.Count == 0)
        {
            // If the old set is now empty, remove it
            // Remove the set from the gameBoardValidSets dictionary
            // O(1)
            cardToSetPos.Remove(key);
            gameBoardValidSets.Remove(oldSetPos);
        }
        // If the card was removed from the middle of the set, split the set into two

    }

    // hash function 
    public int GetKeyFromPosition(CardPosition cardPosition)
    {
        return (cardPosition.Row * 100) + cardPosition.Column;
    }


    public async Task PutInSet(Card card)
    {
        Dictionary<SetPosition, CardsSet> gameBoardValidSets = board.GetGameBoardValidSetsTable();
        Dictionary<int, SetPosition> cardToSetPos = board.GetCardsToSetsTable();
        int key = GetKeyFromPosition(card.Position);
        int rightKey = key + 1;
        int leftKey = key - 1;
        if (cardToSetPos.ContainsKey(rightKey) && cardToSetPos.ContainsKey(leftKey))
        {
            // Combine two sets gameBoardValidSets[cardToSetPos[leftKey]].set and gameBoardValidSets[cardToSetPos[rightKey].set]
            SetPosition leftSetPos = cardToSetPos[leftKey];
            SetPosition rightSetPos = cardToSetPos[rightKey];
            CardsSet leftSet = gameBoardValidSets[leftSetPos];
            CardsSet rightSet = gameBoardValidSets[rightSetPos];
            cardToSetPos[GetKeyFromPosition(rightSet.GetLastCard().Position)] = leftSetPos;
            leftSet.AddCardToEnd(card);
            leftSet.Combine(leftSet, rightSet);
            gameBoardValidSets.Remove(rightSetPos);
            if (rightKey != GetKeyFromPosition(leftSet.GetLastCard().Position))
            {
                cardToSetPos.Remove(rightKey);
            }
            if (leftKey != GetKeyFromPosition(leftSet.GetFirstCard().Position))
            {
                cardToSetPos.Remove(leftKey);
            }
        }
        else if (cardToSetPos.ContainsKey(rightKey))
        {
            // Add card to the beginning of the set
            SetPosition setPos = cardToSetPos[rightKey];
            gameBoardValidSets[setPos].AddCardToBeginning(card);
            if (gameBoardValidSets[setPos].set.Count != 2)
            {
                cardToSetPos.Remove(rightKey);
            }
            //added worked 
            cardToSetPos[key] = setPos;
        }
        else if (cardToSetPos.ContainsKey(leftKey))
        {
            // Add card to the end of the set
            SetPosition setPos = cardToSetPos[leftKey];
            gameBoardValidSets[setPos].AddCardToEnd(card);

            if (gameBoardValidSets[setPos].set.Count != 2)
            {
                cardToSetPos.Remove(leftKey);
            }
            cardToSetPos[key] = setPos;
        }
        else
        {
            // Create a new set
            SetPosition newSetPos = new SetPosition(board.GetSetCountAndInc());
            gameBoardValidSets[newSetPos] = new CardsSet(card);

            cardToSetPos[key] = newSetPos;
        }
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
            sum += card.Number;
        }
        return sum;
    }

    // Return instance of rummikub deck

    public bool IsBoardValid()
    {
        Dictionary<SetPosition, CardsSet> gameBoardValidSets = board.GetGameBoardValidSetsTable();

        bool humanCheck = true; gameController.GetCurrentPlayer().GetInitialMove(); // if the human has made the initial move
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

    public int GetEmptySlotIndexFromGameBoard(int amount)
    {
        GameObject BoardGrid = GameObject.FindGameObjectWithTag("BoardGrid");

        int rowCount = Constants.MaxBoardRows;
        int colCount = Constants.MaxBoardColumns;
        int[] emptySlotsCount = new int[rowCount]; // Array to store the count of consecutive empty slots on each row

        // Iterate through the board and count consecutive empty slots on each row
        for (int i = 0; i < BoardGrid.transform.childCount; i++)
        {
            GameObject currentSlot = BoardGrid.transform.GetChild(i).gameObject;
            int row = i / colCount;

            // Check if the slot is empty
            if (currentSlot.transform.childCount == Constants.EmptyTileSlot)
            {
                emptySlotsCount[row]++;
                // Check if we have found 'amount' consecutive empty slots on the same row
                if (emptySlotsCount[row] == amount + 1 && (i - amount) % Constants.MaxBoardColumns == 0)
                {
                    return i - amount;
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

    internal async Task PlayCardSetOnBoard(CardsSet cardsSet)
    {
        int tileslot = GetEmptySlotIndexFromGameBoard(cardsSet.set.Count + 1);
        tileslot++;
        foreach (Card card in cardsSet.set)
        {
            if (gameController.GetCurrentPlayer().IsCardInList(card))
            {
                //print in green Card in the player hand
                print("<color=Green>Card in the player hand: " + card.ToString() + "</color>");
            }
            else
            {
                // print in red
                print("<color=Red>Card not in the player hand: " + card.ToString() + "</color>");

            }
            card.OldPosition = card.Position;
            card.Position.SetTileSlot(tileslot);
            uiManager.MoveCardToBoard(card, tileslot, true);
            tileslot++;
            // in case of manual undo keep track of the logic for the computer even tho we allow only valid moves
            AddCardToMovesStack(card);
            // move and remove the card
            await MoveCardFromPlayerHandToGameBoard(card);
        }
        print("*******************************************************After play card set on board*******************************************************");
        foreach (Card card in cardsSet.set)
        {
            if (gameController.GetCurrentPlayer().IsCardInList(card))
            {
                //print in green Card in the player hand
                print("<color=Red>Card in the player hand: " + card.ToString() + "</color>");
            }
            else
            {
                // print in green
                print("<color=Green>Card not in the player hand: " + card.ToString() + "</color>");

            }
        }

        gameController.GetCurrentPlayer().PrintCards();

    }

    // Play a card on the board at a specific tile slot and remove it from the player's hand, 
    // assume the play is from the player hand
    internal async Task PlayCardOnBoard(Card card, int tileslot, bool canRemove = true)
    {
        // assume already check no nehibors to combine my love
        uiManager.MoveCardToBoard(card, tileslot, true);
        AddCardToMovesStack(card);
        await MoveCardFromPlayerHandToGameBoard(card, canRemove);
    }
    // Rearrange the cards on the board with the given card to the end or the beginning of the set
    // while keeping the sets valid and the board rules with visual update
    internal async Task RearrangeCardsSet(SetPosition setPosition, Card givenCard, bool addAtTheEnd)
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
}