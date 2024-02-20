using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyDeckException : Exception
{
    public EmptyDeckException() : base("The deck is empty. Cannot draw a card from an empty deck.")
    {
    }
}

