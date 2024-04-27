using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board 
{
    private Dictionary<int, SetPosition> cardToSetPos;
    private Dictionary<SetPosition, CardsSet> gameBoardValidSets;

    public Dictionary<int, SetPosition> GetCardsToSetsTable() => this.cardToSetPos;
    public Dictionary<SetPosition, CardsSet> GetGameBoardValidSetsTable() => this.gameBoardValidSets;

    public Board()
    {
        this.cardToSetPos = new Dictionary<int, SetPosition>();
        this.gameBoardValidSets = new Dictionary<SetPosition, CardsSet>();
    }
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
    }





}
