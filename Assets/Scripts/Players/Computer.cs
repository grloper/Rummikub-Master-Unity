using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.Unicode;

public class Computer : Player
{

    // game board reference
    [HideInInspector] private GameBoard gameBoard;
    // Reference to the ui manager 
    [HideInInspector] private UImanager uiManager;
    // Reference to the game controller
    [HideInInspector] private GameController gameController;
    // The delay for the computer move
    public float computerMoveDelay = 0.2f;
    // Player reference
    private Player myPlayer;
    private bool added; // added single cards
    private bool dropped; // dropped valid sets
    private bool partial; // dropped partial sets with free cards

    // O(n) where n is the number of cards in hand
    private List<CardsSet> ExtractMaxValidRunSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            // order of sorted list: 1,2,3,3,1,2
            // if the same card appears
            if (list[i].Number == list[i - 1].Number &&
                list[i].Color == list[i - 1].Color)
            {
                list.Add(list[i]);
                list.Remove(list[i]);
                // remove the card and add it at the end
            }
            if (list[i].Number == list[i - 1].Number + 1
             && list[i].Color == list[i - 1].Color
             && currentSet.set.Count <= maxRangeInclusive)
            {
                currentSet.AddCardToEnd(list[i]);
            }
            else
            {
                if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive)
                {
                    cardsSets.Add(currentSet);
                }
                currentSet = new CardsSet();
                currentSet.AddCardToEnd(list[i]);
            }
        }
        return cardsSets;
    }
    // O(n) where n is the number of cards in hand
    private List<CardsSet> ExtractMaxValidGroupSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].Number == list[i - 1].Number &&
                list[i].Color == list[i - 1].Color)
            {
                list.Add(list[i]);
                list.Remove(list[i]);
            }
            if (list[i].Number == list[i - 1].Number
                && !currentSet.IsContainThisColor(list[i].Color)
                 && currentSet.set.Count <= maxRangeInclusive)
            {
                currentSet.AddCardToEnd(list[i]);
            }

            else
            {
                if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive)
                {
                    cardsSets.Add(currentSet);
                }
                currentSet = new CardsSet();
                currentSet.AddCardToEnd(list[i]);
            }
        }
        return cardsSets;
    }


    public void MaximizeValidDrops()
    {
        this.dropped = false;
        List<CardsSet> setsRun = ExtractMaxValidRunSets(myPlayer.GetPlayerHand().SortedByRun(), Constants.MinInRun, Constants.MaxInRun);
        if (setsRun.Count > 0)
        {
            this.dropped = true;
            foreach (CardsSet set in setsRun)
            {
                gameBoard.PlayCardSetOnBoard(set);
            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(myPlayer.GetPlayerHand().SortedByGroup(), Constants.MinInGroup, Constants.MaxInGroup);
        if (setsGroup.Count > 0)
        {
            this.dropped = true;
            foreach (CardsSet set in setsGroup)
            {
                gameBoard.PlayCardSetOnBoard(set);
            }
        }

    }
    public void Initialize(Player player)
    {
        this.myPlayer = player; // set the player reference
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>(); // get the game controller reference, to call the methods
        this.uiManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UImanager>(); // get the ui manager reference, to call the methods 
        this.gameBoard = GameObject.FindGameObjectWithTag("BoardGrid").GetComponent<GameBoard>(); // get the game board reference, to call the methods
    }

    public IEnumerator ComputerMove()
    {
        yield return new WaitForSeconds(computerMoveDelay); // wait for the delay
        // Call the method inside Computer.cs named "DoComputerMove"
        DoComputerMove();

    }

    private void DoComputerMove()
    {
        MaximizeValidDrops();
        bool append=false;
        if (myPlayer.GetInitialMove()) //if the player is allowed to append cards to the board
        {
            // perform the following actions until the computer has no moves to make
            do
            {
                if (partial || added)
                    append = true;
                MaximizePartialDrops();
            AssignFreeCardsToExistsSets();
            }
            while(partial||added);
        }
        else//if the player is not allowed to append cards to the board
        {
            if (gameBoard.GetMovesStackSum() < Constants.MinFirstSet) //he must drop more than 30 in sets
            {
                uiManager.DrawACardFromDeck();
                return;
            }
        }
        if (dropped || append) //if the computer has made a move, or have sets to drop.
        {
            uiManager.ConfirmMove(); // confirm the move 
        }
        else
        {
            uiManager.DrawACardFromDeck(); // draw a card if the computer has no moves to make
        }
    }

    public void MaximizePartialDrops()
    {
        this.partial = false;
        print("Partial drops");
        List<CardsSet> setsRun = ExtractMaxValidRunSets(myPlayer.GetPlayerHand().SortedByRun(), Constants.MaxPartialSet, Constants.MaxPartialSet);
        foreach (CardsSet set in setsRun)
        {

            CardInfo info = FindExtractableCardsFromBoard(set);
            if (info != null)
            {
                //this.partial = true;
                ExtractCardAndReArrange(info, set);
            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(myPlayer.GetPlayerHand().SortedByGroup(), Constants.MaxPartialSet, Constants.MaxPartialSet);
        foreach (CardsSet set in setsGroup)
        {
            CardInfo info = FindExtractableCardsFromBoard(set);
            if (info != null)
            {
                //this.partial = true;
                ExtractCardAndReArrange(info, set);
            }
        }
    }

    private void ExtractCardAndReArrange(CardInfo info, CardsSet set)
    {
        print("Found for: " + set.ToString() + ", the card: " + info.GetCard().ToString() + "From set: " + gameBoard.board.GetGameBoardValidSetsTable()[info.GetSetPosition()]);

    }

    private CardInfo FindExtractableCardsFromBoard(CardsSet partialSet)
    {
        foreach (SetPosition sp in gameBoard.board.GetGameBoardValidSetsTable().Keys)
        {
            CardsSet setInBoard = gameBoard.board.GetGameBoardValidSetsTable()[sp];
            if (setInBoard.GetDeckLength() > Constants.MinInRun)
            {
                int count = 0;
                if (setInBoard.GetDeckLength() == Constants.MaxInGroup && setInBoard.isGroupOfColors)
                {
                    foreach (Card card in setInBoard.set)
                    {
                        if (partialSet.CanAddCardFirst(card) || partialSet.CanAddCardLast(card))
                        {
                            return new CardInfo(card, sp, AddPosition.End, count);
                        }
                        count++;
                    }
                }
                else if (setInBoard.GetDeckLength() > 3 && setInBoard.isRun)
                {

                    if (partialSet.CanAddCardLast(setInBoard.GetFirstCard()))
                    {
                        return new CardInfo(setInBoard.GetFirstCard(), sp, AddPosition.End, 0);
                    }
                    else if (partialSet.CanAddCardFirst(setInBoard.GetFirstCard()))
                    {
                        return new CardInfo(setInBoard.GetFirstCard(), sp, AddPosition.Beginning, 0);
                    }
                    else if (partialSet.CanAddCardLast(setInBoard.GetLastCard()))
                    {
                        return new CardInfo(setInBoard.GetLastCard(), sp, AddPosition.End, setInBoard.GetDeckLength() - 1);
                    }
                    else if (partialSet.CanAddCardFirst(setInBoard.GetLastCard()))
                    {
                        return new CardInfo(setInBoard.GetLastCard(), sp, AddPosition.Beginning, setInBoard.GetDeckLength() - 1);
                    }
                    else if (setInBoard.GetDeckLength() > Constants.MinSetLengthForMiddleBreak)
                    {
                        foreach (Card card in setInBoard.GetMiddleCards())
                        {
                            if (partialSet.CanAddCardLast(card))
                            {
                                return new CardInfo(card, sp, AddPosition.Middle, card.Number - setInBoard.GetFirstCard().Number);
                            }
                            else if (partialSet.CanAddCardFirst(card))
                            {
                                return new CardInfo(card, sp, AddPosition.Middle, card.Number - setInBoard.GetFirstCard().Number);

                            }
                        }

                    }

                }
            }
        }
        return null;
    }

    // Assigns free cards to existing sets on the game board, rearrange visualy if needed.
    // O(n*m) where n is the number of cards in the player hand and m is the number of sets on the game board
    public void AssignFreeCardsToExistsSets()
    {
        this.added = false; // reset the added flag, for the confirmation
        List<Card> cardsToRemove = new List<Card>(); // list of cards to remove from the player hand, the cards we assigned to the game board
        foreach (Card card in myPlayer.GetPlayerHand()) // iterate over the player hand
        {
            bool found = false; // flag to indicate if we found a set to add the card to
            int offset = -1; // offset for the middle run
            List<SetPosition> keys = gameBoard.board.GetGameBoardValidSetsTable().Keys.ToList(); // get the keys of the sets on the game board
            keys.ToArray(); // convert the keys to an array
            for (int i = 0; i < keys.Count && !found; i++) // iterate over the keys
            {
                SetPosition key = keys[i]; // get the key for the current iteration
                CardsSet set = gameBoard.board.GetGameBoardValidSetsTable()[key]; // get the current set from the game board
                if (set.CanAddCardLast(card)) // if the card can be added to the end of the set
                {
                    found = true; // indicate that we found a set to add the card to, break the loop
                    this.added = true; // indicate that we added a card for the confirmation
                    cardsToRemove.Add(card); // add the card to the list of cards to remove
                    if (gameBoard.IsSpaceForCard(true, set)) // if there is space for the card in the game board play with O(1)
                    {
                        MoveCardToNextSlot(card, set.GetLastCard().Position.GetTileSlot() + 1); // move the card to the next slot, given true - play without removing the card from the player hand (we handle that at the end of the method), O(1)
                    }
                    else
                    {
                        RearrangeCardsSet(key, card, AddPosition.End); // rearrange the cards in the set, 
                    }
                }
                else if (set.CanAddCardFirst(card)) // if the card can be added to the beginning of the set
                {
                    found = true; // indicate that we found a set to add the card to, break the loop
                    this.added = true; // indicate that we added a card for the confirmation
                    cardsToRemove.Add(card); // add the card to the list of cards to remove
                    if (gameBoard.IsSpaceForCard(false, set)) // if there is space for the card in the game board play with O(1)
                    {
                        MoveCardToPreviousSlot(card, set.GetFirstCard().Position.GetTileSlot() - 1); // move the card to the previous slot, given false - play without removing the card from the player hand (we handle that at the end of the method), O(1)
                    }
                    else
                    {
                        RearrangeCardsSet(key, card, AddPosition.Beginning);
                    }
                }
                else if ((offset = set.CanAddCardMiddleRun(card)) != -1) // if the card can be added to the middle of the run
                {
                    found = true; // indicate that we found a set to add the card to, break the loop
                    this.added = true; // indicate that we added a card for the confirmation
                    cardsToRemove.Add(card); // add the card to the list of cards to remove
                    SplitAndRearrangeCardsSet(key, card, offset); // split the set and rearrange the cards in the game board, O(n) where n is the number of cards in the set
                }
            }
        }
        RemoveCardsFromPlayerHand(cardsToRemove); // remove the cards from the player hand after assigning them to the game board
    }

    // Moves the card to the next slot in the game board, O(1)
    private void MoveCardToNextSlot(Card card, int nextSlot)
    {
        // Save the old position of the card
        card.OldPosition = card.Position;
        // Set the new position of the card
        card.Position.SetTileSlot(nextSlot);
        // Play the card on the board, given false - play without removing the card from the player hand (we handle that at the end of the method)
        gameBoard.PlayCardOnBoard(card, nextSlot, RemoveOption.DontRemove);
    }

    // Moves the card to the previous slot in the game board, O(1)
    private void MoveCardToPreviousSlot(Card card, int previousSlot)
    {
        // Save the old position of the card
        card.OldPosition = card.Position;
        // Set the new position of the card
        card.Position.SetTileSlot(previousSlot);
        // Play the card on the board, given false - play without removing the card from the player hand (we handle that at the end of the method)
        gameBoard.PlayCardOnBoard(card, previousSlot, RemoveOption.DontRemove);
    }

    // Rearranges the cards in the game board, O(n) where n is the number of cards in the set
    private void RearrangeCardsSet(SetPosition key, Card card, AddPosition addPosition)
    {
        gameBoard.RearrangeCardsSet(key, card, addPosition); // rearrange the cards in the set, given isAddingToLast - adding the card at the end of the set
    }

    // Splits the set and rearranges the cards in the game board, O(n) where n is the number of cards in the set
    private void SplitAndRearrangeCardsSet(SetPosition key, Card card, int offset)
    {
        // Get the set from the game board 
        CardsSet set = gameBoard.board.GetGameBoardValidSetsTable()[key];
        // Uncombine the set into two sets, the first one with the offset (appending at the end the remaining card), the second one with the rest of the cards
        CardsSet newSet = set.UnCombine(offset);
        // Create a new set position with new id
        SetPosition newSetPos = new SetPosition(gameBoard.board.GetSetCountAndInc());
        // Add the new set to the game board
        gameBoard.board.AddCardsSet(newSetPos, newSet);
        // Update the keys of the cards in the set
        gameBoard.board.UpdateKeyMultiCardsSet(gameBoard.GetKeyFromPosition(newSet.GetFirstCard().Position), gameBoard.GetKeyFromPosition(newSet.GetLastCard().Position), newSetPos);
        gameBoard.board.UpdateKeyMultiCardsSet(gameBoard.GetKeyFromPosition(set.GetFirstCard().Position), gameBoard.GetKeyFromPosition(set.GetLastCard().Position), key);
        // Rearrange the set with the new card
        gameBoard.RearrangeCardsSet(newSetPos, card, AddPosition.End);
    }

    // Removes the cards from the player hand after assigning them to the game board
    private void RemoveCardsFromPlayerHand(List<Card> cardsToRemove)
    {
        foreach (Card card in cardsToRemove) // iterate over the cards to remove
        {
            myPlayer.RemoveCardFromList(card); // remove the card from the player hand
        }
    }
}