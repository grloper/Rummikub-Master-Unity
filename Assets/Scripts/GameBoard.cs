using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    private List<Card> humanHand = new List<Card>(); // List of cards in the HumanHand's hand
    private List<Card> computerHand = new List<Card>(); // List of cards in the computer's hand
    private List<Card> gameBoardValidSets = new List<Card>(); // List of valid sets on the board
    private RummikubDeck rummikubDeck = new RummikubDeck(); // Deck of cards

    public void AddCardToHumanHand(Card card)
    {
        humanHand.Add(card);
    }

    public List<Card> GetHumanHand()
    {
        return humanHand;
    }

    public void AddCardToComputerHand(Card card)
    {
        computerHand.Add(card);
    }

    public List<Card> GetComputerHand()
    {
        return computerHand;
    }

    //function that check if the set is a Run, same color and consecutive numbers at least 3 numbers up to 13 it is getting a list of card use List property
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

    //function that check if the set is a group, same number and different color at least 3 Colors up to 4 it is getting a list of card use List property the set count is 3 to 4
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

    public RummikubDeck GetRummikubInstance()
    {
        return this.rummikubDeck;
    }

    // Start is called before the first frame update
    void Start()
    {
     
    }

    // Update is called once per frame
    void Update()
    {

    }
}
