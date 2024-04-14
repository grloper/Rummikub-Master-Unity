
// Represents the position of a card on the board
public class CardPosition
{

    public CardPosition()
    {
        // Default constructor
        this.Row = -1;
        this.Column = -1;
    }
    public CardPosition(int emptySlotIndex)
    {
        this.Row = emptySlotIndex / Constants.MaxBoardColumns;
        this.Column = emptySlotIndex % Constants.MaxBoardColumns;
    }


    // constructor for row and column
    public CardPosition(int row, int column)
    {
        this.Row = row;
        this.Column = column;
    }

    // Row and Column of the card
    public int Row { get; set; }
    public int Column { get; set; }



}
