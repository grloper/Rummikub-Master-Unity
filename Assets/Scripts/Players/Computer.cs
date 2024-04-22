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
    private List<Card> computerHand;
    public float computerMoveDelay =2f;// 0.9f;
    // Player reference
    private Player myPlayer;
    private bool added;
    private bool dropped;

    private List<CardsSet> ExtractMaxValidRunSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        SortByRun();
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            // order of sorted list: 1,2,3,3,1,2
            // if the same card appears
            if (list[i].Equals(list[i - 1]))
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
    private List<CardsSet> ExtractMaxValidGroupSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        SortByGroup();
        print("--------------------ExtractMaxValidGroupSets----------------------");
        PrintCards();
        List<CardsSet> cardsSets = new List<CardsSet>();
        CardsSet currentSet = new CardsSet();
        currentSet.AddCardToEnd(list[0]);
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].Equals(list[i - 1]))
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


    public async Task MaximizeValidDrops()
    {
        this.dropped = false;
        List<CardsSet> setsRun = ExtractMaxValidRunSets(this.computerHand, Constants.MinInRun, Constants.MaxInRun);
        if (setsRun.Count > 0)
        {
            this.dropped = true;
            foreach (CardsSet set in setsRun)
            {
              await  gameBoard.PlayCardSetOnBoard(set);
              PrintCards();
            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(this.computerHand, Constants.MinInGroup, Constants.MaxInGroup);
        if (setsGroup.Count > 0)
        {
            this.dropped = true;
            foreach (CardsSet set in setsGroup)
            {
                await gameBoard.PlayCardSetOnBoard(set);
                PrintCards();
            }
        }

    }
    public void Initialize(Player player)
    {
        this.myPlayer = player;
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
        this.uiManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UImanager>();
        this.gameBoard = GameObject.FindGameObjectWithTag("BoardGrid").GetComponent<GameBoard>();
    }

    public IEnumerator ComputerMove()
    {
        yield return new WaitForSeconds(computerMoveDelay); // wait for the delay
        // Call the method inside Computer.cs named "DoComputerMove"
        DoComputerMove();

    }

    private async void DoComputerMove()
    {
        this.computerHand = myPlayer.GetPlayerHand();
        await MaximizeValidDrops();
        //  MaximizePartialDrops();
        added = false;
      //  await AssignFreeCardsToExistsSets();
        if (dropped || added)
        {
            uiManager.ConfirmMove();
        }
        else
        {
            uiManager.DrawACardFromDeck();
        }
    }
    private async Task AssignFreeCardsToExistsSets()
    {
        // track the cards that need to be removed from the computer hand because 
        // cant remove them while iterating over the list
        List<Card> cardsToRemove = new List<Card>();
        // iterate over the computer hand
        foreach (Card card in this.computerHand)
        {
            // if added a card to a set then break the loop
            bool found = false;
            // extract the keys from the dictionary to iterate over them
            List<SetPosition> keys = gameBoard.GetGameBoardValidSetsTable().Keys.ToList();
            keys.ToArray();
            // Create a copy of the keys
            for (int i = 0; i < keys.Count&&!found; i++)
            {
                // get the set from the dictionary
                SetPosition key = keys[i];
                // get the current set from the dictionary using the SetPosition key
                CardsSet set = gameBoard.GetGameBoardValidSetsTable()[key];
                // check if the card can be added to the set without needing to move it
                int tileslot = -1;

                // if we keeping a set valid then we need to update the keys in the dictionary and add the card
                if (set.CanAddCardEndRun(card) || set.CanAddCardEndGroup(card))
                {
                    // tell the computer that it added a card to a set (need to confirm at the end of the turn instead of drawing)
                    // get the second tile slot to check if it is empty
                    GameObject secondTileSlot = null;
                    // if the last card in the set is not in the last column
                    if (set.GetLastCard().Position.Column != Constants.MaxBoardColumns - 1)
                    {
                        // get the second tile slot
                        secondTileSlot = this.gameBoard.transform.GetChild(set.GetLastCard().Position.GetTileSlot() + 2).gameObject;
                         // 1,2,3, _, this
                    
                    // if the second tile slot is empty then add the card to the set
                    if (secondTileSlot != null && secondTileSlot.transform.childCount == Constants.EmptyTileSlot||card.Position.Column==Constants.MaxBoardColumns-2)
                    {
                        // update the card position to the next tile slot 
                        card.OldPosition = card.Position;
                        card.Position.SetTileSlot(set.GetLastCard().Position.GetTileSlot() + 1);
                        // add the card to the set
                        await gameBoard.PlayCardOnBoard(card, set.GetLastCard().Position.GetTileSlot() + 1, false);
                        // mark the the card is already in a set
                        cardsToRemove.Add(card);
                          this.added = true;
                          found=true;
                       
                    } 
                    }// rearrange the set to add the card
                    // else
                    // {
                    //     // need to move the whole set to a free spot
                    //     tileslot = gameBoard.GetEmptySlotIndexFromGameBoard(set.GetDeckLength() + 1);
                    //     //<
                    //     print("1 set.set: " + set.set.Count);
                    //     foreach (Card cardInSet in set.set)
                    //     {
                    //         print("before: "+cardInSet.ToString());
                    //         //give the cards their new positions
                    //         cardInSet.OldPosition = cardInSet.Position;
                    //         cardInSet.Position.SetTileSlot(tileslot);
                    //         print("after: "+cardInSet.ToString());
                    //         // move them visualy to the new spots
                    //         uiManager.MoveCardToBoard(cardInSet, tileslot);
                    //         print("after after: "+cardInSet.ToString());
                    //         await gameBoard.MoveCardFromGameBoardToGameBoard(cardInSet);
                    //         tileslot++;
                    //     } //>
                    //     // update the card position to be the last tile slot
                    //     card.OldPosition = card.Position;
                    //     card.Position.SetTileSlot(tileslot);
                    //     // update the keys in the dictionary to suit the new location
                    //    // UpdateKeysAfterAddingCard(set, key);
                    //     // move the card to the board also visualy and mark to not delete it inside the function
                    //     await gameBoard.PlayCardOnBoard(card, set.GetLastCard().Position.GetTileSlot() + 1, false);
                    //     found = true;
                    //     cardsToRemove.Add(card);

                    // }
                }
                else if (!found && (set.CanAddCardBegginingRun(card) || set.CanAddCardBegginingGroup(card)))
                {
                    GameObject secondTileSlot = null;
                    // if the first card in the set is not in the first column
                    if (set.GetFirstCard().Position.Column != 0)
                    {
                        // get the second tile slot
                        secondTileSlot = this.gameBoard.transform.GetChild(set.GetFirstCard().Position.GetTileSlot() - 2).gameObject; // this, _ ,1,2,3

                        // if the second tile slot is empty then add the card to the set
                        if (secondTileSlot != null && secondTileSlot.transform.childCount == Constants.EmptyTileSlot||card.Position.Column==1)
                        {
                            card.Position.SetTileSlot(set.GetFirstCard().Position.GetTileSlot() - 1);
                            await gameBoard.PlayCardOnBoard(card, set.GetFirstCard().Position.GetTileSlot() - 1, false);
                            cardsToRemove.Add(card);
                        this.added = true;
                        found=true;
                        }
                    }
                    // rearrange the set to add the card
                    // else
                    // {

                    //     // get the first empty slot in the game board to suit that amount of cards(the set and the new card)
                    //     tileslot = gameBoard.GetEmptySlotIndexFromGameBoard(set.GetDeckLength() + 1);
                    //     print("tileslot: " + tileslot);
                    //     // first give the card that position because he is at the begining
                    //     card.OldPosition = card.Position;
                    //     card.Position.SetTileSlot(tileslot);
                    //     print("card position: " + card.Position.ToString());
                    //     print("2 set.set: " + set.set.Count);
                    //     foreach (Card cardInSet in set.set)
                    //     {
                    //         // update the set position to the new tile slot starting from the second tileslot given before
                    //         // the first tileslot is saved for the new card
                    //         tileslot++;
                    //         print("before: "+cardInSet.ToString());
                    //         cardInSet.OldPosition = cardInSet.Position;
                    //         cardInSet.Position.SetTileSlot(tileslot);
                    //         print("after: "+cardInSet.ToString());
                    //         // move the cards visualy to the new spots
                    //         uiManager.MoveCardToBoard(cardInSet, tileslot);
                    //         print("after after: "+cardInSet.ToString());
                    //         await gameBoard.MoveCardFromGameBoardToGameBoard(cardInSet);
                    //     }
                    //     // update the keys to suit the new start and end location to point to that set
                    //    // UpdateKeysAfterAddingCard(set, key);
                    //     // move the card to the board also visualy and mark to not delete it inside the function
                    //     await gameBoard.PlayCardOnBoard(card, set.GetFirstCard().Position.GetTileSlot() -1, false);
                    //     cardsToRemove.Add(card);
                    //     found = true;
                    //     //remove the old once

                    // }

                }
            }
        }
        foreach (Card card in cardsToRemove)
        {
            this.computerHand.Remove(card);
        }
    }

    private void UpdateKeysAfterAddingCard(CardsSet set, SetPosition setPosition)
    {
        int newLeftkey = gameBoard.GetKeyFromPosition(set.GetFirstCard().Position);
        int newRightkey = gameBoard.GetKeyFromPosition(set.GetLastCard().Position);
        gameBoard.GetCardsInSetsTable()[newLeftkey] = setPosition;
        gameBoard.GetCardsInSetsTable()[newRightkey] = setPosition;
    }

    public void PrintCards()
    {
        print("------------------------------------------Set:------------------------------------------");
        foreach (Card card in computerHand)
        {
            print(card.ToString());
        }
    }

    // Sort the cards by group
    public void SortByGroup()
    {
        computerHand.Sort((card1, card2) =>
        {
            if (card1.Number == card2.Number)
                return card1.Color.CompareTo(card2.Color); // group by same number and differnt colors (duplicates beside eachother)
            else
                return card1.Number.CompareTo(card2.Number);
        });

    }


    // Sort the cards by run 
    public void SortByRun()
    {
        computerHand.Sort((card1, card2) =>
        {
            if (card1.Color == card2.Color)
                return card1.Number.CompareTo(card2.Number);
            else
                return card1.Color.CompareTo(card2.Color);
        });
    }



}