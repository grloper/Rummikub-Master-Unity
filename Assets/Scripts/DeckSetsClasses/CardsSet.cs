using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// This class is used to represent a set of cards on the board. with many utility methods to check if the set is a run or a group of colors and more
public class CardsSet : ICardSet
{
    // a certain set of cards on board
    public DoublyLinkedList<Card> set; // the set of the connected cards
    public bool isRun; // if the set represents a run, min 3 cards, max 13 cards
    public bool isGroupOfColors; // if the set represents a group of colors, (3 or 4 cards)
    // Default Constructor, initialize the set of cards
    public CardsSet()
    {
        set = new DoublyLinkedList<Card>();
        isRun = false;
        isGroupOfColors = false;
    }
    // Constructor for a set of cards with a single card in it uses the default constructor
    public CardsSet(Card card) : this()
    {
        set.AddFirst(card);
    }
    // Constructor for a set of cards with a list of cards in it
    public CardsSet(CardsSet cardsSet)
    {
        // deep copy the set of cards
        set = new DoublyLinkedList<Card>(); // create a new set of cards
        Node<Card> currentNode = cardsSet.set.GetFirstNode(); // get the first node of the set
        while (currentNode != null) // loop through the set of cards and add them to the new set
        {
            set.AddLast(currentNode.Value); // add the card to the new set
            currentNode = currentNode.Next; // move to the next card
        }
        // copy the isRun and isGroupOfColors
        isRun = cardsSet.isRun;
        isGroupOfColors = cardsSet.isGroupOfColors;
    }
    // O(1) 
    public int GetDeckLength()
    {
        return set.Count;
    }
    // Utility methods
    //O(1)
    public Card GetFirstCard() => set.GetFirstNode().Value; // get the first card in the set
    // O(1)
    public Card GetLastCard() => set.GetLastNode().Value; // get the last card in the set
    // O(1)
    public void AddCardToBeginning(Card card) => set.AddFirst(card); // add a card to the beginning of the set
    // O(1)
    public void AddCardToEnd(Card card) => set.AddLast(card); // add a card to the end of the set
    // O(1)
    public bool IsContainsCard(Card card)
    {
        return set.Contains(card); // check if the set contains a certain card
    }
    // Combine two sets of cards and return the new set of cards, O(1)
    public CardsSet Combine(CardsSet set1, CardsSet set2)
    {
        //append set2 to set2 while both contain set.set which is DoublyLinkedList<Card>
        set1.set.Append(set2.set);
        return set1;
    }
    // Uncombine the set of cards and return the new set of cards,
    // the offset is the number of cards to remove from the set because they are at another list
    // O(n) when n is the number of cards to remove
    public CardsSet UnCombine(int offset)
    {
        CardsSet newSet = new CardsSet(); // create a new set of cards (returned set)
        for (int i = 0; i < offset; i++)
        {
            newSet.set.AddLast(set.GetFirstNode().Value); // add the card to the new set
            set.RemoveFirst();
        }
        return newSet;
    }

    // Remove a card from the set and return its index in the set
    // O(n) when n is the number of cards in the set
    public int RemoveCard(Card card)
    {
        Node<Card> current = set.GetFirstNode(); // get the first node of the set
        int i = 0; // the index of the card
        while (current != null) // loop through the set of cards till the end
        {
            if (current.Value.Equals(card)) // if the card is found
            {
                set.Remove(current); // remove the card from the set
                return i; // return the index of the card
            }
            current = current.Next; // move to the next card
            i++; // increment the index
        }
        return -1; // return -1 if the card is not found
    }
    // Check if a run is valid
    // O(n) when n is the number of cards in the set
    public bool IsRun()
    {
        if (set == null || set.Count < Constants.MinInRun || set.Count > Constants.MaxInRun)
            return isRun = false; // A run must have at least 3 cards but no more than 13
        Node<Card> node = GetFirstNodeOfNotJoker(); // get the first node of a card that is not a joker
        CardColor SetColor = node.Value.Color; // get the color of the set
        int CurrentNum = node.Value.Number; // get the number of the first card
        if(IsJoker(GetFirstCard())&& CurrentNum == Constants.MinRank)
        {
            return isRun = false;
        }
        for (Node<Card> current = node; current != null; current = current.Next)
        {
            if (CurrentNum == Constants.MaxRank + 1 || // the number is bigger than 13
               CurrentNum++ != current.Value.Number && !IsJoker(current.Value) || // the number is not in sequence (we also check for jokers = 0xf)
                !IsSameColor(current.Value, SetColor)) // the color is not the same
                return isRun = false;
        }
        return isRun = true;
    }



    // get the first node of a card that is not a joker
    // O(1) when n is max number of jokers in a row which is 2
    public Node<Card> GetFirstNodeOfNotJoker()
    {
        Node<Card> node = set.GetFirstNode(); // get the first node of the set
        for (int i = 0; i <= Constants.MaxJoker && node != null; i++, node = node.Next) // loop through the set of cards till amount of possible jokers in a row
            if (!IsJoker(node.Value)) // if the card is not a joker
                return node; // return the node
        return null; // Default to null if all are jokers
    }

    // Check if a set contains a certain color
    // O(n) when n is the number of cards in the set
    public bool IsContainThisColor(CardColor c)
    {
        Node<Card> current = set.GetFirstNode(); // get the first node of the set
        while (current != null) // loop through the set of cards till the end
        {
            if (current.Value.Color == c) // if the color is found
                return true; // return true
            current = current.Next; // move to the next card
        }
        return false; // return false if the color is not found
    }
    // check if the set is a valid group of colors
    // O(n) when n is the number of cards in the set
    public bool IsGroupOfColors()
    {
        if (set == null || (set.Count != Constants.MinInGroup && set.Count != Constants.MaxInGroup))
        {
            return isGroupOfColors = false; // A group must have either 3 or 4 cards
        }
        Node<Card> node = GetFirstNodeOfNotJoker(); // get the first node of a card that is not a joker
        int CurrentNum = node.Value.Number; // get the number of the first card
        HashSet<int> distinctColors = new HashSet<int>();// Use a HashSet to track distinct colors
        for (Node<Card> current = node; current != null; current = current.Next) // loop through the set of cards till the end
            if (!IsJoker(current.Value) && (!distinctColors.Add((int)current.Value.Color) // if the color is repeated
             || CurrentNum != current.Value.Number)) //or the number is not the same
                return isGroupOfColors = false; 
        return isGroupOfColors = true;
    }
    // check if a card is the same color as the given color, if joker return true, O(1)
    public bool IsSameColor(Card c1, CardColor color)
    {
        return IsJoker(c1) || c1.Color == color;
    }
    // check if two cards are consecutive, if joker return true, O(1)
    public bool IsConsicutive(Card c1, Card c2)
    {
        return IsJoker(c1) || c1.Number == c2.Number + 1;
    }
    // check if a card is a joker and return true if it is,O(1)
    public bool IsJoker(Card card)
    {
        return card.Number == Constants.JokerRank; // the joker is a mask of 1111b
    }
    // simple override for the ToString method
    public override string ToString()
    {
        Node<Card> current = set.GetFirstNode();
        string setStr = "";
        while (current != null)
        {
            setStr += current.Value.ToString() + ", ";
            current = current.Next;
        }
        return setStr;
    }





    // minimize adding calling at first or end
    public bool CanAddCardFirst(Card card)
    {
        return CanAddCardBeginningRun(card) || CanAddCardBeginningGroup(card);
    }
    public bool CanAddCardLast(Card card)
    {
        return CanAddCardEndRun(card) || CanAddCardEndGroup(card);
    }
 
    // check if a card can add to the beginning of the set to create a group of colors
     public bool CanAddCardBeginningGroup(Card card)
    {
        AddCardToBeginning(card);
        bool check = this.IsGroupOfColors();
        this.set.RemoveFirst();
        return check;

    }
    // check if a card can add to the beginning of the set to create a run, O(1)
      public bool CanAddCardBeginningRun(Card card)
    {
        AddCardToBeginning(card);
        bool check= this.IsRun();
        this.set.RemoveFirst();
        return check;
        
    }
    // check if a card can add to the end of the set to create a group of colors
      public bool CanAddCardEndGroup(Card card)
    {
        AddCardToEnd(card);
        bool check = this.IsGroupOfColors();
        this.set.RemoveLast();
        return check;
    }
    // check if a card can be added to the end of the set to create a run, O(1)
    public bool CanAddCardEndRun(Card card)
    {
        //add the card to the set O(1)
        AddCardToEnd(card);
        bool check = this.IsRun();
        //remove the card from the set O(1)
        this.set.RemoveLast();
        return check;
    }

    // O(1)
    // return the index where the card should be if can be added. otherside return -1
    public int CanAddCardMiddleRun(Card card)
    {
        // Check if the set is long enough and the card has the same color O(1)
        if (set.Count < Constants.MinSetLengthForMiddleRun || card.Color != set.GetFirstNode().Value.Color)
            return -1;
        // Check if the card can be added in the middle O(1)
        if (card.Number >= set.GetFirstNode().Value.Number + Constants.MiddleRunOffset
         && card.Number <= set.GetLastNode().Value.Number - Constants.MiddleRunOffset)
        {
            // if the card can be added in the middle, return the index of the card
            return card.Number - set.GetFirstNode().Value.Number; //because the set is a run
        }
        return -1;
    }

    internal IEnumerable<Card> GetMiddleCards()
    {
        int getAmount = set.Count - 6;
        return set.Skip(3).Take(getAmount);
    }

    // End of CardsSet.cs
}