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
    // Stack of cards that were moved so we can undo the move
    private Stack<Card> playerMovesStack = new Stack<Card>();
    // refeence to the HumanGrid
    [SerializeField] GameObject HumanGrid;
    // Reference to Human so we can draw cards
    [SerializeField] Human human;

    // Start is called before the first frame update
    private void Start()
    {
        ExplainGameRules();
    }

    public void UndoPlayerMoves()
    {
        while (playerMovesStack.Count > 0)
        {
            Card card = playerMovesStack.Pop();
            UndoMoveForCard(card);
        }
    }
    private void UndoMoveForCard(Card card)
    {
        // Undo logic for a single card

        MoveCardFromGameBoardToHumanHand(card.GetComponent<Card>());
        int emptySlotIndex = human.GetEmptySlotIndex();
        GameObject tileSlot = HumanGrid.transform.GetChild(emptySlotIndex).gameObject;
        card.transform.SetParent(tileSlot.transform);
        card.transform.localPosition = Vector3.zero;
    }
    // add card to the stack of moves that the player made so we can undo them
    public void AddCardToPlayerStack(Card card)
    {
        playerMovesStack.Push(card);
    }
    
    public Stack<Card> GetPlayerMovesStack()
    {
        return playerMovesStack;
    }

    // Move Card from GameBoard to HumanHand
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



    public void MoveCardFromHumanHandToGameBoard(Card card)
    {
        humanHand.Remove(card);

        // Find the appropriate set based on the card's position
        List<Card> targetSet = FindTargetSet(card.Position);

        // Add the card to the target set
        targetSet.Add(card);

        PrintGameBoardValidSets();
    }

    private List<Card> FindTargetSet(CardPosition cardPosition)
    {
        foreach (var set in gameBoardValidSets)
        {
            // If the set is empty, add the card to it
            if (set.Count == 0)
                return set;

            // Get the position of the last card in the set
            CardPosition lastCardPosition = set[set.Count - 1].Position;

            // Check if the new card is in the same column or consecutive column
            if (cardPosition.Column == lastCardPosition.Column + 1)
                return set;
        }

        // If no suitable set is found, create a new set
        List<Card> newSet = new List<Card>();
        gameBoardValidSets.Add(newSet);
        return newSet;
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
        Debug.Log("The game is played with two sets of 52 cards and 2 jokers. Each player has 14 cards in his hand. The goal of the game is to get rid of all the cards in your hand. You can do this by creating sets of cards. There are two types of sets: a group and a run. A group is a set of 3 or 4 cards with the same number but different colors. A run is a set of 3 or more cards with the same color and consecutive numbers. A joker can be used as any card. You can add cards to the sets on the board or create new sets. You can also move cards inside the boards as long as you are not breaking the rules and keeps all the sets valids.");
    }

    // Check if the set is a run 
    public bool IsRun(List<Card> set)
    {
        // Check if the set is consecutive numbers and has the same color
        set.Sort((a, b) => a.Number.CompareTo(b.Number));
        CardColor firstCardColor = set[0].Color;

        for (int i = 1; i < set.Count; i++)
        {
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
            foreach (var card in set)
            {
                Debug.Log(card.ToString());
            }

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
