using System;

public class UndoException : Exception
{
    public UndoException() : base("No Moves To Undo.")
    {
    }
}