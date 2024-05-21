using System;


public class EmptyDeckException : Exception
{
    public EmptyDeckException() : base("The deck is empty. Cannot draw a card from an empty deck.")
    {
    }
}

