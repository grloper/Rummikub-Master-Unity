using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node<T>
{
    public T Value { get; set; }
    public Node<T> Next { get; set; }
    public Node<T> Prev { get; set; }
}
