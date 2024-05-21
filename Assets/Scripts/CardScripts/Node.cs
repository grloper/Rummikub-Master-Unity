using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is used to create a node for a doubly linked list. 
public class Node<T>
{
    // simple outline of a node.
    public T Value { get; set; } // value of the node
    public Node<T> Next { get; set; } // next node
    public Node<T> Prev { get; set; } // previous node
}
