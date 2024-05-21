using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants
{

    // Global constants
    public const int EmptyStack = 0;
    public const int EmptyCardsSet = 0;

    // Board UI constants
    public const int MaxPlayerColumns = 20;
    public const int MaxBoardColumns = 29;
    public const int MaxBoardRows = 8;
    public const int EmptyTileSlot = 0;
    public const int MaxGameBoardSlots = 232;
    public const int MaxPlayerBoardSlots = 40;

    // Player constants
    public const int CardsToDraw = 14;

    // RummikubDeck constants
    public const int MaxRank = 13;
    public const int MaxSuit = 4;
    public const int EmptyDeck = 0;
    public const int JokerRank = 0xf;
    public const int MaxJoker = 2;

    // CardSets constants
    public const int MaxInRun = 13;
    public const int MaxInGroup = 4;
    public const int MinInGroup = 3;
    public const int MinInRun = 3;
    public const int MinFirstSet = 30;

    // Gameboard constants
    public const int ZeroCardsSet = 0;

    // Partial set constants
    public const int MaxPartialSet = 2;
    public const int EmptyPartialSet = 0;
    public const int SingleCardInPartialSet = 1;
    // appending card in the middle constants
    public const int MinSetLengthForMiddleRun = 5;
    public const int MiddleRunOffset = 2;
    // breaking cards from the middle constants
    public const int MinSetLengthForMiddleBreak = 6;
    

}
