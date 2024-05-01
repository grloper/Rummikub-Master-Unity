using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board
{
    // represent an instance of the visual board logicly

    // the cardToSetPos dictionary is used to map between the card position on the board and the set position it belongs to
    private readonly Dictionary<int, SetPosition> cardToSetPos;

    // the gameBoardValidSets dictionary is used to map between the set position and the cards set that belongs to it
    private readonly Dictionary<SetPosition, CardsSet> gameBoardValidSets;

    // the SetCount is used to give each set a unique id
    private int SetCount;
    public Dictionary<int, SetPosition> GetCardsToSetsTable() => this.cardToSetPos;
    public Dictionary<SetPosition, CardsSet> GetGameBoardValidSetsTable() => this.gameBoardValidSets;
    public int GetSetCountAndInc()
    {
        return this.SetCount++;
    }

    public Board()
    {
        this.cardToSetPos = new Dictionary<int, SetPosition>();
        this.gameBoardValidSets = new Dictionary<SetPosition, CardsSet>();
        this.SetCount = 0;

    }
    // deep copy constructor to work with the undo feature
    public Board(Board board)
    {
        // deep copy the cardToSetPos and gameBoardValidSets
        this.cardToSetPos = new Dictionary<int, SetPosition>();
        foreach (KeyValuePair<int, SetPosition> pair in board.cardToSetPos)
        {
            this.cardToSetPos[pair.Key] = new SetPosition(pair.Value.GetId());
        }
        // deep copy the gameBoardValidSets
        this.gameBoardValidSets = new Dictionary<SetPosition, CardsSet>();
        foreach (KeyValuePair<SetPosition, CardsSet> pair in board.gameBoardValidSets)
        {
            // deep copy the CardsSet without reference help
            this.gameBoardValidSets[pair.Key] = new CardsSet(pair.Value);
        }
        this.SetCount = board.SetCount;
    }
    private int GetKeyFromPosition(CardPosition cardPosition)
    {
        return (cardPosition.Row * 100) + cardPosition.Column;
    }

    public SetPosition FindCardSetPosition(Card card)
    {
        // Remove the card from its current set
        int row = card.OldPosition.Row;
        int col = card.OldPosition.Column;
        int key = GetKeyFromPosition(card.OldPosition);
        if (!this.cardToSetPos.ContainsKey(key))
        {
            // Move left
            //O(n) whee n is the number of cards in the set 
            for (int i = col; i >= 0; i--)
            {
                key = row * 100 + i;
                if (this.cardToSetPos.ContainsKey(key))
                {
                    return this.cardToSetPos[key];
                }
            }
        }
        else
        {
            //O(1)
            return cardToSetPos[key];
        }
        return null;
    }

    public CardsSet GetCardsSet(SetPosition setPosition)
    {
        if (this.gameBoardValidSets.ContainsKey(setPosition))
        {
            return this.gameBoardValidSets[setPosition];
        }
        return null;
    }

    public void RemoveSetFromBothDic(int key)
    {
        this.gameBoardValidSets.Remove(cardToSetPos[key]);
        this.cardToSetPos.Remove(key);
    }

    public void AddCardsSet(SetPosition setPosition, CardsSet cardsSet)
    {
        this.gameBoardValidSets.Add(setPosition, cardsSet);
    }

    //O(1)
    public void HandleBeginningAndEndKeysUpdate(Card card, CardsSet oldSet, SetPosition oldSetPos, int cardIndex)
    {
        // if the card is the first card in the set
        if (cardIndex == 0)
        {
            // remove the old set from the cardToSetPos dictionary
            this.cardToSetPos.Remove(GetKeyFromPosition(card.OldPosition));
            // add the new set to the cardToSetPos dictionary
            this.cardToSetPos[GetKeyFromPosition(oldSet.GetFirstCard().Position)] = oldSetPos;
        }// if the card is the last card in the set
        else if (cardIndex == oldSet.set.Count)
        {// remove the old set from the cardToSetPos dictionary
            this.cardToSetPos.Remove(GetKeyFromPosition(card.OldPosition));
            // add the new set to the cardToSetPos dictionary
            this.cardToSetPos[GetKeyFromPosition(oldSet.GetLastCard().Position)] = oldSetPos;
        }
    }

    
    public void HandleMiddleSplit(Card card, CardsSet oldSet, SetPosition oldSetPos, int cardIndex)
    {
        // new set = the set from the left 
        // set from the right is the old set with the card removed, oldSetPos
        CardsSet newSet = oldSet.UnCombine(cardIndex);
        SetPosition newSetPos = new SetPosition(SetCount++);
        AddCardsSet(newSetPos, newSet);

        if (newSet.set.Count > 1)
        {
            //upate the first card and last card to point to that newSetPos
            cardToSetPos[GetKeyFromPosition(newSet.GetFirstCard().Position)] = newSetPos;
            cardToSetPos[GetKeyFromPosition(newSet.GetLastCard().Position)] = newSetPos;
        }
        else
        {
            cardToSetPos[GetKeyFromPosition(newSet.GetFirstCard().Position)] = newSetPos;

        }
        if (oldSet.set.Count > 1)
        {
            // update the last card in the old set to point to the oldSetPos
            cardToSetPos[GetKeyFromPosition(oldSet.GetFirstCard().Position)] = oldSetPos;
            cardToSetPos[GetKeyFromPosition(oldSet.GetLastCard().Position)] = oldSetPos;
        }
        else
        {
            cardToSetPos[GetKeyFromPosition(oldSet.GetFirstCard().Position)] = oldSetPos;

        }
    }

    public bool CardKeyExistsInSet(int key)
    {
        return cardToSetPos.ContainsKey(key);
    }

    public SetPosition GetSetPosition(int key)
    {
        return cardToSetPos[key];
    }

    public void UpdateSetPosition(CardsSet set, SetPosition sp_key)
    {
        cardToSetPos[GetKeyFromPosition(set.GetLastCard().Position)] = sp_key;
    }

    public void RemoveValidSet(SetPosition st_key)
    {
        gameBoardValidSets.Remove(st_key);
    }
    public void RemoveSetPosition(int key)
    {
        cardToSetPos.Remove(key);
    }
    public void SetSetPosition(int key, SetPosition sp)
    {
        cardToSetPos[key] = sp;
    }

    public void CreateNewSet(Card card, int key)
    {
        SetPosition newSetPos = new SetPosition(this.GetSetCountAndInc());
        AddCardsSet(newSetPos, new CardsSet(card));
        cardToSetPos[key] = newSetPos;
    }
}
