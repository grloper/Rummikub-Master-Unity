using System;


public class WinException : Exception
{
    public WinException(string v) : base("The Player has won the game.")
    {
    }
}

