using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    // List of cards in the HumanHand's hand
    private List<Card> humanHand = new List<Card>(); 
    // List of cards in the computer's hand
    private List<Card> computerHand = new List<Card>(); 
    // List of valid sets on the board
    private List<Card> gameBoardValidSets = new List<Card>(); 
     // Single Instance! Deck of cards
    private RummikubDeck rummikubDeck = new RummikubDeck();

    // Move Card from GameBoard to HumanHand
    public void MoveCardFromGameBoardToHumanHand(Card card)
    {
        gameBoardValidSets.Remove(card);
        humanHand.Add(card);
    }


    //Move Card from HumanHand to GameBoard
    public void MoveCardFromHumanHandToGameBoard(Card card)
    {
        humanHand.Remove(card);
        gameBoardValidSets.Add(card);
    }
    // Move Card from ComputerHand to GameBoard
    public void MoveCardFromComputerHandToGameBoard(Card card)
    {
        computerHand.Remove(card);
        gameBoardValidSets.Add(card);
    }

    // Add Card to HumanHand
    public void AddCardToHumanHand(Card card)
    {
        humanHand.Add(card);
    }

    // return instance of human hand
    public List<Card> GetHumanHand()
    {
        return humanHand;
    }

    // Add Card to ComputerHand
    public void AddCardToComputerHand(Card card)
    {
        computerHand.Add(card);
    }

    // return instance of computer hand
    public List<Card> GetComputerHand()
    {
        return computerHand;
    }

    // explain the game rules
    public void ExplainGameRules()
    {
        Debug.Log("The game is played with two sets of 52 cards and 2 jokers. Each player has 14 cards in his hand. The goal of the game is to get rid of all the cards in your hand. You can do this by creating sets of cards. There are two types of sets: a group and a run. A group is a set of 3 or 4 cards with the same number but different colors. A run is a set of 3 or more cards with the same color and consecutive numbers. A joker can be used as any card. You can add cards to the sets on the board or create new sets. You can also move cards inside the boards as long as you are not breaking the rules and keeps all the sets valids.");
    }
    
    // check if the set is a run 
    public bool IsRun(List<Card> set) 
    {
        // Check if the set is a run
        bool isSameColor = true;
        bool isConsecutiveNumbers = true;

        // Check if the set is the same color
        CardColor firstCardColor = set[0].Color;
        foreach (Card card in set)
        {
            if (card.Color != firstCardColor)
            {
                isSameColor = false;
                break;
            }
        }

        // Check if the set is consecutive numbers
        set.Sort((a, b) => a.Number.CompareTo(b.Number));
        for (int i = 1; i < set.Count; i++)
        {
            if (set[i].Number != set[i - 1].Number + 1)
            {
                isConsecutiveNumbers = false;
                break;
            }
        }

        // Check if the set is at least 3 numbers up to 13
        bool isAtLeastThreeNumbers = set.Count >= 3;
        bool isUpToThirteen = set.Count <= 13;

        // Return true if the set is a run
        if (isSameColor && isConsecutiveNumbers && isAtLeastThreeNumbers && isUpToThirteen)
        {
            return true;
        }

        // Return false if the set is not a run
        return false;
    } 

    // check if the set is a group of colors
    public bool IsGroupOfColors(List<Card> set)
    {
        // Check if the set is a group
        bool isSameNumber = true;
        bool isDifferentColor = true;

        // Check if the set is the same number
        int firstCardNumber = set[0].Number;
        foreach (Card card in set)
        {
            if (card.Number != firstCardNumber)
            {
                isSameNumber = false;
                break;
            }
        }

        // Check if the set is different color
        CardColor firstCardColor = set[0].Color;
        foreach (Card card in set)
        {
            if (card.Color == firstCardColor)
            {
                isDifferentColor = false;
                break;
            }
        }

        // Check if the set is at least 3 colors up to 4
        bool isAtLeastThreeColors = set.Count >= 3;
        bool isUpToFour = set.Count <= 4;

        // Return true if the set is a group
        if (isSameNumber && isDifferentColor && isAtLeastThreeColors && isUpToFour)
        {
            return true;
        }

        // Return false if the set is not a group
        return false;
    }

    // return instance of rummikub deck
    public RummikubDeck GetRummikubDeckInstance()
    {
        return this.rummikubDeck;
    }
}
