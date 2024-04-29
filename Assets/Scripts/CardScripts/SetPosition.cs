
public class SetPosition
{
    private readonly int id;

    public SetPosition(int id)
    {
        this.id = id;
    }
    public int GetId()
    {
        return this.id;
    }
    //equals method because when getting the key from the dictionary, we need to compare the card position 
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