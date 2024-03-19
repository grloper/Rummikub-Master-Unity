
using System.Collections.Generic;


public class CardsSet : ICardSet
{
    // a certain set of cards on board
    public List<Card> set;
    public bool isRun;
    public bool isGroupOfColors;
    // 3-13 cards in a set if it represents a run 
    // 3-4 cards in a set if it represents a group of colors
    // Default Constructor
    public CardsSet()
    {
        set = new List<Card>();
    }
    // Constructor for a set of cards with a single card in it uses the default constructor
    public CardsSet(Card card) : this()
    {
        set.Add(card);
    }

    // Utility methods
    public Card GetFirstCard() => set[0];

    public Card GetLastCard() => set[set.Count - 1];


    //add card beggining and end in two function 
    public void AddCardToBeggining(Card card)
    {
        if (set.Count == Constants.EmptyCardsSet)
        {
            set.Add(card);
            return;
        }
        set.Insert(0, card);
    }
    public void AddCardToEnd(Card card)
    {
        if (set.Count == Constants.EmptyCardsSet)
        {
            set.Add(card);
            return;
        }
        set.Add(card);
    }

    // can add at the begginging of the set of List<Card> return bool true it check if the postion of the card colum + 1 is equal to the first card postion colum
    public bool CanAddCardBeggining(Card card)
    {
        return GetFirstCard().Position.Column == card.Position.Column + 1;
    }
    // can add at the end of the set of List<Card> return bool true it check if the postion of the card colum - 1 is equal to the last card postion colum
    public bool CanAddCardEnd(Card card)
    {
        return GetLastCard().Position.Column == card.Position.Column - 1;
    }
    // check if the set contains a certain card
    public bool IsContainsCard(Card card)
    {
        return set.Contains(card);
    }
    // Combine two sets of cards and return the new set of cards
    public CardsSet Combine(CardsSet set1, CardsSet set2)
    {
        foreach (Card c in set2.set)
        {
            set1.set.Add(c);
        }
        // Sort the set by column positio, we assume that the cards are already sorted by row position
        set1.set.Sort((x, y) => x.Position.Column.CompareTo(y.Position.Column));
        set2.set.Clear();
        return set1;
    }
    // Uncombine the set of cards and return the new set of cards,
    // the offset is the number of cards to remove from the set because they are at another list
    public CardsSet UnCombine(int offset)
    {
        CardsSet newSet = new CardsSet();
        for (int i = 0; i < offset; i++)
        {
            newSet.set.Add(set[i]);
        }
        set.RemoveRange(0, offset);
        return newSet;
    }

    // Remove a card from the set and return its index in the set
    public int RemoveCard(Card card)
    {
        int i = set.FindIndex(c => c == card);
        set.Remove(card);
        return i;
    }


    // Check if a run is valid
    public bool IsRun()
    {
        if (set == null || set.Count < Constants.MinInRun || set.Count > Constants.MaxInRun)
        {
            return isRun = false; // A run must have at least 3 cards but no more than 13
        }
        int i = GetFirstIndexOfNotJoker();

        CardColor SetColor = set[i].Color;
        int CurrentNum = set[i].Number;
        if (set[i].Number < i + 1)
            return isRun = false;

        for (; i < set.Count; i++)
        {
            if (CurrentNum == Constants.MaxRank + 1 || // the number is bigger than 13
               CurrentNum++ != set[i].Number && !IsJoker(set[i]) || // the number is not in sequence (we also check for jokers = 0xf)
                !IsSameColor(set[i], SetColor)) // the color is not the same
            {
                return isRun = false;
            }
        }
        return isRun = true;
    }
    // Check if a group of colors is valid
    public bool IsGroupOfColors()
    {
        if (set == null || (set.Count != Constants.MinInGroup && set.Count != Constants.MaxInGroup))
        {
            return isGroupOfColors = false; // A group must have either 3 or 4 cards
        }

        int i = GetFirstIndexOfNotJoker();
        int CurrentNum = set[i].Number;
        // Use a HashSet to track distinct colors
        HashSet<int> distinctColors = new HashSet<int>();

        for (; i < set.Count; i++)
        {
            if (!IsJoker(set[i]) && (!distinctColors.Add((int)set[i].Color) || CurrentNum != set[i].Number))
            {
                return isGroupOfColors = false; // If a color is repeated, the group is invalid
            }
        }

        return isGroupOfColors = true;
    }
    // check if a card is the same color as the given color, if joker return true
    public bool IsSameColor(Card c1, CardColor color)
    {
        return IsJoker(c1) || c1.Color == color;
    }
    public bool IsConsicutive(Card c1, Card c2)
    {
        return IsJoker(c1) || c1.Number == c2.Number + 1;
    }
    // check if a card is a joker and return true if it is
    public bool IsJoker(Card card)
    {
        return card.Number == Constants.JokerRank; // the joker is a mask of 1111b
    }
    // get the first index of a card that is not a joker
    public int GetFirstIndexOfNotJoker()
    {
        for (int i = 0; i <= Constants.MaxJoker; i++)
        {
            if (!IsJoker(set[i]))
            {
                return i;
            }
        }

        return 2; // Default to the last index if all are jokers
    }
    public override string ToString()
    {
        string setStr = "";
        foreach (Card c in set)
        {
            setStr += c.ToString() + ", ";
        }
        return setStr;
    }

}
