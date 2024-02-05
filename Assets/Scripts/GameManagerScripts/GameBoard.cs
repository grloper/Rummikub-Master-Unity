using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UIElements;

public class GameBoard : MonoBehaviour
{
    // List of cards in the HumanHand's hand
    private List<Card> humanHand = new List<Card>();
    // List of cards in the computer's hand
    private List<Card> computerHand = new List<Card>();
    // List of valid sets on the board
    private List<List<CardsSet>> gameBoardValidSets = new List<List<CardsSet>>();
    // Single Instance! Deck of cards
    private RummikubDeck rummikubDeck = new RummikubDeck();
    // Stack of moves that the player made so we can undo them
    private Stack<Card> movesStack = new Stack<Card>();
    // refeence to the HumanGrid
    [SerializeField] GameObject HumanGrid;
    // refeence to the BoardGrid
    [SerializeField] GameObject BoardGrid;
    // Reference to Human so we can draw cards
    [SerializeField] Human human;

    // Start is called before the first frame update
    private void Start()
    {
        //game board valid sets need to have at max 0-7 sets which is the rows on board and every set can have many CardsSet which will held by location on board
        gameBoardValidSets = new List<List<CardsSet>>();
        for (int i = 0; i < 8; i++)
        {
            gameBoardValidSets.Add(new List<CardsSet>());
        }
        
        ExplainGameRules();
    }
    public void UndoMoves()
    {
        while (movesStack.Count > 0)
        {
            Card card = movesStack.Pop();
            UndoMoveForCard(card);
        }
        PrintGameBoardValidSets();
    }


    public bool IsExistForStack(Card card)
    {
        return movesStack.Contains(card);
    }
    private void UndoMoveForCard(Card card)
    {
        // Undo logic for a single card
        if (!card.CameFromHumanHand)
        {
            
            // Move the card back to its original position
            DraggableItem draggableItem = card.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                Debug.Log("<color=yellow>from board to board undo</color>");
                // change the position of the card to match the logic of the PutInSet() called in the MoveCardFromGameBoardToGameBoard
                CardPosition tempPos = card.Position;
                card.Position = card.OldPosition;
                card.OldPosition = tempPos;
                MoveCardFromGameBoardToGameBoard(card);
                //card.ParentBeforeDrag is the tile slot location on board when pushed first to the stack
                draggableItem.parentAfterDrag =card.ParentBeforeDrag;
                card.transform.SetParent(draggableItem.parentAfterDrag);
                card.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // working
            Debug.Log("<color=yellow>from board to human undo</color>");

            MoveCardFromGameBoardToHumanHand(card);
            int emptySlotIndex = human.GetEmptySlotIndex();
            GameObject tileSlot = HumanGrid.transform.GetChild(emptySlotIndex).gameObject;
            card.transform.SetParent(tileSlot.transform);
            card.transform.localPosition = Vector3.zero;
        }
    }
    // add card to the stack of moves   
    public void AddCardToMovesStack(Card card)
    {
        movesStack.Push(card);
    }

    public Stack<Card> GetMovesStack()
    {
        return movesStack;
    }

    // Move Card from GameBoard to GameBoard
    public void MoveCardFromGameBoardToHumanHand(Card card)
    {
        foreach (CardsSet cardsSet in gameBoardValidSets[card.Position.Row])
        {
            if (cardsSet.IsContainsCard(card))
            {
                cardsSet.RemoveCard(card);
                if (cardsSet.set.Count == 0)
                {
                    gameBoardValidSets[card.Position.Row].Remove(cardsSet);
                }
                break;
            }
        }
        humanHand.Add(card);
    }

    // Print all items in gameBoardValidSets
    public void PrintGameBoardValidSets()
    {
        Debug.Log("<color=red>Print Sets:</color>");
        int count = 0;
        foreach (var set in gameBoardValidSets)
        {
            //print this in white Debug.Log("Set " + count + ":"); 
            Debug.Log("<color=white>Set " + count + ":</color>");
            foreach (var item in set)
            {
               //print this in orange Debug.Log(item.ToString());
               Debug.Log("<color=orange>" + item.ToString() + "</color>");
            }
            count++;
        }
    }
    // worked without changing the postion when calling from UndoMoveForCard
    //public void MoveCardFromGameBoardToGameBoardUndo(Card card)
    //{

    //    //print old postions and postions in red and green
    //    Debug.Log("<color=red>Old Position: " + card.OldPosition.Row + ", " + card.OldPosition.Column + "</color>");
    //    Debug.Log("<color=green>New Position: " + card.Position.Row + ", " + card.Position.Column + "</color>");
    //    foreach (CardsSet cardsSet in gameBoardValidSets[card.Position.Row])
    //    {
    //        if (cardsSet.IsContainsCard(card))
    //        {
    //            cardsSet.RemoveCard(card);
    //            if (cardsSet.set.Count == 0)
    //            {
    //                gameBoardValidSets[card.Position.Row].Remove(cardsSet);
    //            }
    //            break;
    //        }
    //    }
    //    CardPosition tempPos =card.Position;
    //    card.Position = card.OldPosition;
    //    card.OldPosition = tempPos;
    //    PutInSet(card);
    //}
    public void MoveCardFromGameBoardToGameBoard(Card card)
    {

        //print old postions and postions in red and green
        Debug.Log("<color=red>Old Position: " + card.OldPosition.Row + ", " + card.OldPosition.Column + "</color>");
        Debug.Log("<color=green>New Position: " + card.Position.Row + ", " + card.Position.Column + "</color>");
        foreach (CardsSet cardsSet in gameBoardValidSets[card.OldPosition.Row])
        {
            if (cardsSet.IsContainsCard(card))
            {
                cardsSet.RemoveCard(card);
                if (cardsSet.set.Count == 0)
                {
                    gameBoardValidSets[card.OldPosition.Row].Remove(cardsSet);
                }
                break;
            }
        }
        PutInSet(card);
    }
   
    public void MoveCardFromHumanHandToGameBoard(Card card)
    {
        PutInSet(card);
        humanHand.Remove(card);
    }
    //complexity O(n^2) when n is the number of sets in the row 
    public void PutInSet(Card card)
    {
        //add card in the row postion in a new CardsSet
        if (gameBoardValidSets[card.Position.Row].Count == 0)
        {
            //add the new CardSet to the set
            gameBoardValidSets[card.Position.Row].Add(new CardsSet(card));
        }
        else
        {
            foreach(CardsSet cardSet in gameBoardValidSets[card.Position.Row])
            {

                if (cardSet.CanAddCardBeggining(card))
                {
                    cardSet.AddCardBeggining(card);
                    Combine(card.Position);
                    return;
                }
                else if(cardSet.CanAddCardEnd(card))
                {
                    cardSet.AddCardEnd(card);
                    Combine(card.Position);
                    return;
                }
            }
                gameBoardValidSets[card.Position.Row].Add(new CardsSet(card));
        }
    }
    

    public void Combine(CardPosition cardPosition)
    {

        foreach (CardsSet cardsSet in gameBoardValidSets[cardPosition.Row])
        {
            //try to find two sets that can combine and combine them
            foreach (CardsSet cardsSet1 in gameBoardValidSets[cardPosition.Row])
            {
                if (cardsSet != cardsSet1)
                {
                    // cardset1 is the right of cardset 
                    if (cardsSet.LastCard().Position.Column == cardsSet1.FirstCard().Position.Column - 1)
                    {
                        cardsSet.Combine(cardsSet, cardsSet1);
                        //remove it
                        gameBoardValidSets[cardPosition.Row].Remove(cardsSet1);
                        return;
                    }
                    // cardset1 is the left of cardset
                    else if (cardsSet.FirstCard().Position.Column == cardsSet1.LastCard().Position.Column + 1)
                    {
                        cardsSet1.Combine(cardsSet1, cardsSet);
                        //remove it
                        gameBoardValidSets[cardPosition.Row].Remove(cardsSet);
                        return;
                    }
                }
            }
        }
    }

 


    // Move Card from ComputerHand to GameBoard
    public void MoveCardFromComputerHandToGameBoard(Card card)
    {
        //locate the set and remove it from it
        foreach (List<CardsSet> set in gameBoardValidSets) // set to a variable of type List<CardsSet>
        {
            foreach (var item in set) // item to a variable of type CardsSet
            {
                if (item.IsContainsCard(card))// if the set contains the card
                {
                    item.RemoveCard(card); // remove the card from the set

                    if (item.set.Count == 0)
                        gameBoardValidSets.Remove(set);// if the set is empty remove it from the board
                    break;
                }
            }
        }
        //FuncAdd
    }

    // Add Card to HumanHand
    public void AddCardToHumanHand(Card card)
    {
        humanHand.Add(card);
    }

    // Return instance of human hand
    public List<Card> GetHumanHand()
    {
        return humanHand;
    }

    // Add Card to ComputerHand
    public void AddCardToComputerHand(Card card)
    {
        computerHand.Add(card);
    }

    // Return instance of computer hand
    public List<Card> GetComputerHand()
    {
        return computerHand;
    }

    // Explain the game rules
    public void ExplainGameRules()
    {
        print("The game is played with two sets of 52 cards and 2 jokers. Each player has 14 cards in his hand. The goal of the game is to get rid of all the cards in your hand. You can do this by creating sets of cards. There are two types of sets: a group and a run. A group is a set of 3 or 4 cards with the same number but different colors. A run is a set of 3 or more cards with the same color and consecutive numbers. A joker can be used as any card. You can add cards to the sets on the board or create new sets. You can also move cards inside the boards as long as you are not breaking the rules and keeps all the sets valids.");
    }

 

    // Return instance of rummikub deck
    public RummikubDeck GetRummikubDeckInstance()
    {
        return this.rummikubDeck;
    }

    public bool IsBoardValid()
    {
        foreach (var set in gameBoardValidSets)
        {
            foreach (var item in set)
            {
                bool isRun = item.IsRun();
                bool isGroup = item.IsGroupOfColors();

                if (!isRun && !isGroup)
                {
                    Debug.Log($"Invalid set: {string.Join(", ", set)}");
                    Debug.Log($"IsRun: {isRun}, IsGroupOfColors: {isGroup}");
                    return false;
                }
            }
          
        }

        return true;
    }


}
