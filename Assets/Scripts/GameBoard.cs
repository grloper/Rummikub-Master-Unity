using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    // List of cards in the HumanHand's hand
    private List<Card> humanHand = new List<Card>();
    // List of cards in the computer's hand
    private List<Card> computerHand = new List<Card>();
    // List of valid sets on the board
    private List<List<Card>> gameBoardValidSets = new List<List<Card>>();
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
        gameBoardValidSets = new List<List<Card>>();
        ExplainGameRules();
    }
    public void UndoMoves()
    {
        while (movesStack.Count > 0)
        {
            Card card = movesStack.Pop();
            UndoMoveForCard(card);
        }
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
                print("from board to board undo");
                MoveCardFromGameBoardToGameBoard(card.GetComponent<Card>());
                //card.ParentBeforeDrag is the tile slot location on board when pushed first to the stack
                draggableItem.parentAfterDrag =card.ParentBeforeDrag;
                card.transform.SetParent(draggableItem.parentAfterDrag);
                card.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            print("from board to human undo");
            MoveCardFromGameBoardToHumanHand(card.GetComponent<Card>());
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
        foreach (var set in gameBoardValidSets)
        {

            if (set.Contains(card))
            {
                set.Remove(card);
                if (set.Count == 0)
                {
                    gameBoardValidSets.Remove(set);
                }
                break;
            }
        }
        humanHand.Add(card);
    }

    // Print all items in gameBoardValidSets
    public void PrintGameBoardValidSets()
    {
        int count = 0;
        foreach (var set in gameBoardValidSets)
        {
            Debug.Log("Set " + count + ":");
            foreach (var card in set)
            {
                Debug.Log(card.ToString());
            }
            count++;
        }
    }
    public void MoveCardFromGameBoardToGameBoard(Card card)
    {
        //remove the card from the set
        foreach (var set in gameBoardValidSets)
        {
            if (set.Contains(card))
            {
                set.Remove(card);
                if (set.Count == 0)
                {
                    gameBoardValidSets.Remove(set);
                }
                break;
            }
        }
        // add the card in the set
        PutInSet(card);
    }

    public void MoveCardFromHumanHandToGameBoard(Card card)
    {
        PutInSet(card);
        humanHand.Remove(card);

    }
    // Comlexity O(n^2) when n is the number of cards in the board max is 11,236 106*106
    public void PutInSet(Card card)
    {
        int indexToInsert = -1;

        // Check if the card is between two existing sets by exactly 1 column and same row
        for (int i = 0; i < gameBoardValidSets.Count - 1; i++)
        {
            Card lastCardSet1 = gameBoardValidSets[i][gameBoardValidSets[i].Count - 1];
            Card firstCardSet2 = gameBoardValidSets[i + 1][0];

            if (card.Position.Row == lastCardSet1.Position.Row &&
                card.Position.Row == firstCardSet2.Position.Row &&
                Math.Abs(card.Position.Column - lastCardSet1.Position.Column) == 1 &&
                Math.Abs(firstCardSet2.Position.Column - card.Position.Column) == 1)
            {
                // Combine the two sets into a single set with the new card in between
                gameBoardValidSets[i].Add(card);
                gameBoardValidSets[i].AddRange(gameBoardValidSets[i + 1]);
                gameBoardValidSets.RemoveAt(i + 1);
                indexToInsert = i;
                break;
            }
        }

        if (indexToInsert == -1)
        {
            // If not between two sets, follow the logic for adding to an existing set or creating a new set
            bool addedToExistingSet = false;

            foreach (List<Card> cardSet in gameBoardValidSets)
            {
                Card firstCard = cardSet[0];
                Card lastCard = cardSet[cardSet.Count - 1];

                if (card.Position.Row == firstCard.Position.Row && card.Position.Column == firstCard.Position.Column - 1)
                {
                    cardSet.Insert(0, card);
                    addedToExistingSet = true;
                    break;
                }
                else if (card.Position.Row == lastCard.Position.Row && card.Position.Column == lastCard.Position.Column + 1)
                {
                    cardSet.Add(card);
                    addedToExistingSet = true;
                    break;
                }
            }

            if (!addedToExistingSet)
            {
                // If not added to an existing set, create a new set for the card
                List<Card> newCardSet = new List<Card> { card };
                gameBoardValidSets.Add(newCardSet);
            }
        }

        // After adding the card, check and combine adjacent sets if needed
        CombineAdjacentSets();
    }

    private void CombineAdjacentSets()
    {
        for (int i = 0; i < gameBoardValidSets.Count - 1; i++)
        {
            Card lastCardSet1 = gameBoardValidSets[i][gameBoardValidSets[i].Count - 1];
            Card firstCardSet2 = gameBoardValidSets[i + 1][0];

            // Check if the last card of the first set and the first card of the second set are successive
            if (lastCardSet1.Position.Row == firstCardSet2.Position.Row &&
                Math.Abs(lastCardSet1.Position.Column - firstCardSet2.Position.Column) == 1)
            {
                // Combine the two sets into a single set
                gameBoardValidSets[i].AddRange(gameBoardValidSets[i + 1]);
                gameBoardValidSets.RemoveAt(i + 1);
                i--; // Move back one index to recheck with the previous set
            }
        }
    }



    // Move Card from ComputerHand to GameBoard
    public void MoveCardFromComputerHandToGameBoard(Card card)
    {
        //locate the set and remove it from it
        foreach (var set in gameBoardValidSets)
        {
            if (set.Contains(card))
            {
                set.Remove(card);
                break;
            }
        }
        gameBoardValidSets.Add(new List<Card> { card });
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

    // Check if the set is a run 
    public bool IsRun(List<Card> set)
    {
        // Check if the set is consecutive numbers and has the same color
      //  set.Sort((a, b) => a.Number.CompareTo(b.Number));
        CardColor firstCardColor = set[0].Color;

        for (int i = 1; i < set.Count; i++)
        {
            if (set[i].Number==14 || set[i-1].Number==14)
            {
                continue;
            }
            if (set[i].Number != set[i - 1].Number + 1 || set[i].Color != firstCardColor)
            {
                return false;
            }
        }

        // Check if the set is at least 3 numbers up to 13
        return set.Count >= 3 && set.Count <= 13;
    }

    // Check if the set is a group of colors
    public bool IsGroupOfColors(List<Card> set)
    {
        // Check if the set is the same number and has different colors
        int firstCardNumber = set[0].Number;
        HashSet<CardColor> uniqueColors = new HashSet<CardColor>();

        foreach (Card card in set)
        {
            if (card.Number == 14)
            {
                continue;
            }
            if (card.Number != firstCardNumber)
            {
                return false;
            }

            if (!uniqueColors.Add(card.Color))
            {
                // Color is not unique
                return false;
            }
        }

        // Check if the set is at least 3 colors up to 4
        return set.Count >= 3 && set.Count <= 4;
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
            bool isRun = IsRun(set);
            bool isGroup = IsGroupOfColors(set);

            if (!isRun && !isGroup)
            {
                Debug.Log($"Invalid set: {string.Join(", ", set)}");
                Debug.Log($"IsRun: {isRun}, IsGroupOfColors: {isGroup}");
                return false;
            }
        }

        return true;
    }


}
