
// SetPosition is a class that represents the position of a set of connected cards in the game board.
public class SetPosition
{
    private readonly int id; // unique id for the each set of cards

    // Constructor for the set position
    public SetPosition(int id)
    {
        this.id = id;
    }
    public int GetId()
    {
        return this.id;
    }
    //equals method because when getting the key from the dictionary, we need to compare the set position 
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        SetPosition otherSetPosition = (SetPosition)obj;
        return id == otherSetPosition.id;
    }
    //hashcode method
    public override int GetHashCode()
    {
        return id;
    }
}