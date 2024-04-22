
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
        //this is from tile index to row and column
        this.Row = emptySlotIndex / Constants.MaxBoardColumns;
        this.Column = emptySlotIndex % Constants.MaxBoardColumns;
    }
    public int GetTileSlot()
    {
        return this.Row * Constants.MaxBoardColumns + this.Column;
    }
    public void SetTileSlot(int tileSlot)
    {
        this.Row = tileSlot / Constants.MaxBoardColumns;
        this.Column = tileSlot % Constants.MaxBoardColumns;
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

    // Equals method
  

}
