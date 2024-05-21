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

        if (myPlayer.GetInitialMove()) //if the player is allowed to append cards to the board
        {
            MaximizePartialDrops();
            AssignFreeCardsToExistsSets();
        }
        else//if the player is not allowed to append cards to the board
        {
            if (gameBoard.GetMovesStackSum() < Constants.MinFirstSet) //he must drop more than 30 in sets
            {
                uiManager.DrawACardFromDeck();
                return;
            }
        }
        if (dropped || added || partial) //if the computer has made a move, or have sets to drop.
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
         int combinedValue;
        print("Partial drops");
        List<CardsSet> setsRun = ExtractMaxValidRunSets(myPlayer.GetPlayerHand().SortedByRun(), Constants.MaxPartialSet, Constants.MaxPartialSet);
        foreach (CardsSet set in setsRun)
        {
            
            CardInfo info = FindExtractableCardsFromBoard(set);
            if (info != null)
            {
                this.partial = true;
                ExtractCardAndReArrange(info);
            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(myPlayer.GetPlayerHand().SortedByGroup(), Constants.MaxPartialSet, Constants.MaxPartialSet);
        foreach (CardsSet set in setsGroup)
        {
            CardInfo info = FindExtractableCardsFromBoard(set);
            if (info !=  null)
            {
                this.partial = true;
                ExtractCardAndReArrange(info);
            }
        }
    }

    private void ExtractCardAndReArrange(CardInfo info)
    {

    }

    private CardInfo FindExtractableCardsFromBoard(CardsSet partialSet)
    {
        foreach (SetPosition sp in gameBoard.board.GetGameBoardValidSetsTable().Keys)
        {
            CardsSet setInBoard = gameBoard.board.GetGameBoardValidSetsTable()[sp];
            if (setInBoard.GetDeckLength() > Constants.MinInRun)
            {
                int count = 0;
                if (setInBoard.GetDeckLength() == 4 && setInBoard.isGroupOfColors)
                {
                    foreach (Card card in setInBoard.set)
                    {
                        if (partialSet.CanAddCardBegginingGroup(card) || partialSet.CanAddCardBegginingRun(card) || partialSet.CanAddCardEndGroup(card) || partialSet.CanAddCardEndRun(card))
                        {
                            return new CardInfo(card, sp, true, count);
                        }
                        count++;
                    }
                }
                else if (setInBoard.GetDeckLength() > 3 && setInBoard.isRun)
                {

                    if (partialSet.CanAddCardEndGroup(setInBoard.GetFirstCard()) || partialSet.CanAddCardEndRun(setInBoard.GetFirstCard()))
                    {
                        return new CardInfo(setInBoard.GetFirstCard(), sp, true, 0);
                    }
                    else if(partialSet.CanAddCardBegginingGroup(setInBoard.GetFirstCard()) || partialSet.CanAddCardBegginingRun(setInBoard.GetFirstCard()))
                    {
                        return new CardInfo(setInBoard.GetFirstCard(), sp, false, 0);
                    }
                    else if (partialSet.CanAddCardEndGroup(setInBoard.GetLastCard()) || partialSet.CanAddCardEndRun(setInBoard.GetLastCard()))
                    {
                        return new CardInfo(setInBoard.GetLastCard(), sp,true,setInBoard.GetDeckLength() - 1);
                    }
                    else if(partialSet.CanAddCardBegginingGroup(setInBoard.GetLastCard()) || partialSet.CanAddCardBegginingRun(setInBoard.GetLastCard()))
                    {
                        return new CardInfo(setInBoard.GetLastCard(), sp, false, setInBoard.GetDeckLength() - 1);
                    }
                    else if (setInBoard.GetDeckLength() > 6)
                    {
                        foreach (Card card in setInBoard.GetMiddleCards())
                        {
                            if (partialSet.CanAddCardEndGroup(card) || partialSet.CanAddCardEndRun(card))
                            {
                               
                                return new CardInfo(card, sp, true, card.Number - setInBoard.GetFirstCard().Number);
                            }
                            else if(partialSet.CanAddCardBegginingGroup(card) || partialSet.CanAddCardBegginingRun(card))
                            {
                             return new CardInfo(card, sp, false, card.Number - setInBoard.GetFirstCard().Number);
  
                            }
                        }

                    }

                }
            }
        }
        return null;
    }

    // Assigns free cards to existing sets on the game board, rearrange visualy if needed.
    public void AssignFreeCardsToExistsSets()
    {
        this.added = false;
        // track the cards that need to be removed from the computer hand because 
        // cant remove them while iterating over the list
        List<Card> cardsToRemove = new List<Card>();
        // iterate over the computer hand
        foreach (Card card in myPlayer.GetPlayerHand())
        {
            // if added a card to a set then break the loop
            bool found = false;
            int offset = -1;//the offset of the card if can be added in the middle of some set and split it into two different sets
                            // extract the keys from the dictionary to iterate over them
            List<SetPosition> keys = gameBoard.board.GetGameBoardValidSetsTable().Keys.ToList();
            keys.ToArray();
            // Create a copy of the keys
            for (int i = 0; i < keys.Count && !found; i++) // iterate over the keys (SetPosition objects) until we find a set to add the card to for some card
            {
                // get the set from the dictionary
                SetPosition key = keys[i];
                // get the current set from the dictionary using the SetPosition key
                CardsSet set = gameBoard.board.GetGameBoardValidSetsTable()[key];
                // check if the card can be added to the set without needing to move it
                // if we keeping a set valid then we need to update the keys in the dictionary and add the card
                if (set.CanAddCardEndRun(card) || set.CanAddCardEndGroup(card))
                {
                    found = true; // break the loop
                    this.added = true;
                    cardsToRemove.Add(card);
                    // if there is space for the card then add it to the set
                    // if there is no space for the card then we need to rearrange the set
                    // forward true means that we need to add the card to the end of the set
                    if (gameBoard.IsSpaceForCard(true, set))
                    {
                        card.OldPosition = card.Position;
                        card.Position.SetTileSlot(set.GetLastCard().Position.GetTileSlot() + 1);
                        gameBoard.PlayCardOnBoard(card, set.GetLastCard().Position.GetTileSlot() + 1, false);
                    }
                    else
                    {
                        // if there is no space for the card then we need to rearrange the set
                        // forward true means that we need to add the card to the end of the set
                        gameBoard.RearrangeCardsSet(key, card, true);
                    }

                }
                else if (set.CanAddCardBegginingRun(card) || set.CanAddCardBegginingGroup(card))
                {
                    found = true; // break the loop
                    this.added = true;
                    cardsToRemove.Add(card);

                    // if there is space for the card then add it to the set
                    // if there is no space for the card then we need to rearrange the set
                    // forward false means that we need to add the card to the beginning of the set
                    if (gameBoard.IsSpaceForCard(false, set))
                    {
                        // save the old position of the card
                        card.OldPosition = card.Position;
                        card.Position.SetTileSlot(set.GetFirstCard().Position.GetTileSlot() - 1);
                        //play the card on the new position
                        gameBoard.PlayCardOnBoard(card, set.GetFirstCard().Position.GetTileSlot() - 1, false);
                    }
                    else
                    {
                        // // if there is no space for the card then we need to rearrange the set
                        // // forward false means that we need to add   the card to the beginning of the set
                        this.gameBoard.RearrangeCardsSet(key, card, false);
                    }
                }
                else if (set.CanAddCardMiddleRun(card) != -1)
                {
                    found = true; // break the loop
                    this.added = true;
                    cardsToRemove.Add(card);
                    offset = set.CanAddCardMiddleRun(card);
                    gameBoard.PrintGameBoardValidSets();
                    CardsSet newSet = set.UnCombine(offset);
                    SetPosition newSetPos = new SetPosition(gameBoard.board.GetSetCountAndInc());
                    gameBoard.board.AddCardsSet(newSetPos, newSet);
                    gameBoard.board.UpdateKeyMultiCardsSet(gameBoard.GetKeyFromPosition(newSet.GetFirstCard().Position), gameBoard.GetKeyFromPosition(newSet.GetLastCard().Position), newSetPos);
                    gameBoard.board.UpdateKeyMultiCardsSet(gameBoard.GetKeyFromPosition(set.GetFirstCard().Position), gameBoard.GetKeyFromPosition(set.GetLastCard().Position), key);
                    gameBoard.RearrangeCardsSet(newSetPos, card, true);
                    gameBoard.PrintGameBoardValidSets();


                }
            }

        }

        foreach (Card card in cardsToRemove)
        {
            print("remove: " + card.ToString());
            myPlayer.RemoveCardFromList(card);
        }

    }

}