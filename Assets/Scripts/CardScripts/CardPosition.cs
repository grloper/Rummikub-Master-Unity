
// Represents the position of a card on the board
public class CardPosition
{

    // Default constructor without parameters
    public CardPosition()
    {
        this.Row = -1;
        this.Column = -1;
    }
    // Constructor for a tile index, tile index = row * number of columns + column, it is used in 1D arrays in unity
    public CardPosition(int emptySlotIndex)
    {
        //this is from tile index to row and column
        this.Row = emptySlotIndex / Constants.MaxBoardColumns; // row is the integer division of the tile slot by the number of columns
        this.Column = emptySlotIndex % Constants.MaxBoardColumns; // column is the remainder of the tile slot divided by the number of columns
    }
    // Get and Set the tile slot
    public int GetTileSlot()
    {
        return this.Row * Constants.MaxBoardColumns + this.Column; // tile slot is row * number of columns + column
    }
    public void SetTileSlot(int tileSlot)
    {
        this.Row = tileSlot / Constants.MaxBoardColumns; // row is the integer division of the tile slot by the number of columns
        this.Column = tileSlot % Constants.MaxBoardColumns; // column is the remainder of the tile slot divided by the number of columns
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

    // to string for row and col
    public override string ToString()
    {
        return "Row: " + Row + " Column: " + Column;
    }
}
