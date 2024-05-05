using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoublyLinkedList<T> : IEnumerable<T>
{
    public Node<T> Head { get; private set; }
    public Node<T> Tail { get; private set; }
    public int Count { get; private set; }
    private readonly HashSet<T> valueSet; // HashSet for constant time contains checks
    private readonly Dictionary<T, Node<T>> nodeDictionary; // Dictionary for constant time node retrieval
    // O(1)
    public DoublyLinkedList()
    {
        Count = 0;
        valueSet = new HashSet<T>();
        nodeDictionary = new Dictionary<T, Node<T>>();
    }
    // O(1)
    public void AddFirst(T value)
    {
        Node<T> node = new Node<T> { Value = value };
        if (Head == null)
        {
            Head = Tail = node;
        }
        else
        {
            node.Next = Head;
            Head.Prev = node;
            Head = node;
        }
        Count++;
        valueSet.Add(value); // Add value to HashSet
        nodeDictionary[value] = node; // Add value-node pair to Dictionary
    }
    // O(1)
    public void AddLast(T value)
    {
        Node<T> node = new Node<T> { Value = value };
        if (Tail == null)
        {
            Head = Tail = node;
        }
        else
        {
            node.Prev = Tail;
            Tail.Next = node;
            Tail = node;
        }

        Count++;
        valueSet.Add(value); // Add value to HashSet
        nodeDictionary[value] = node; // Add value-node pair to Dictionary
    }
    // O(1)
    public Node<T> GetFirstNode()
    {
        return Head;
    }
    // O(1)
    public Node<T> GetLastNode()
    {
        return Tail;
    }
    // Remove the first node in the linked list, O(1)
    public void RemoveFirst()
    {
        if (Head == null)
        {
            return; // Nothing to remove if the list is empty
        }

        if (Head == Tail)
        {
            // If there's only one element in the list
            Head = Tail = null;
        }
        else
        {
            Head = Head.Next;
            if (Head != null)
            {
                Head.Prev = null;
            }
        }
        Count = (Head == null) ? 0 : Count - 1; // Adjust count accordingly

        // Remove the value from the HashSet if Head is not null
        if (Head != null)
        {
            valueSet.Remove(Head.Value);
            nodeDictionary.Remove(Head.Value); // Remove value-node pair from Dictionary
        }
    }
    // Remove the last node in the linked list, O(1)
    public void RemoveLast()
    {
        if (Tail == null)
        {
            return; // Nothing to remove if the list is empty
        }

        if (Head == Tail)
        {
            // If there's only one element in the list
            Head = Tail = null;
        }
        else
        {
            Tail = Tail.Prev;
            if (Tail != null)
            {
                Tail.Next = null;
            }
        }
        Count = (Tail == null) ? 0 : Count - 1; // Adjust count accordingly

        // Remove the value from the HashSet if Tail is not null
        if (Tail != null)
        {
            valueSet.Remove(Tail.Value);
            nodeDictionary.Remove(Tail.Value); // Remove value-node pair from Dictionary
        }
    }
    //Get a node by its value, O(1)
    public Node<T> GetNode(T value)
    {
        if (nodeDictionary.ContainsKey(value))
        {
            return nodeDictionary[value];
        }
        return null;
    }

    // Remove a node from the linked list, O(1)
    public void Remove(Node<T> node)
    {
        if (node == null)
        {
            return;
        }

        if (node == Head)
        {
            RemoveFirst();
        }
        else if (node == Tail)
        {
            RemoveLast();
        }
        else
        {
            // Adjust the pointers to skip over the 'node'
            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;
            Count--;
            // Remove the value from the HashSet
            valueSet.Remove(node.Value);
            nodeDictionary.Remove(node.Value); // Remove value-node pair from Dictionary
        }
    }

    /*You're absolutely right, the foreach loop itself iterates through the elements in the other.nodeDictionary, which could potentially have n key-value pairs (items). However, in this specific context, the overall time complexity of the Append function remains O(1) due to the following reasons:

        Constant Time Dictionary Lookups: Each iteration within the foreach loop involves a dictionary lookup (using kvp[0]) to access the value from the key-value pair. Since dictionaries have O(1) time complexity for lookups, this step doesn't significantly impact the overall complexity.
        Constant Time Operations: The remaining operations within the loop involve adding the value and its corresponding node to your own nodeDictionary, which are also constant time operations.

    Therefore, even though the foreach loop iterates through the key-value pairs, the constant time nature of dictionary lookups and the remaining operations within the loop ensure that the overall Append function maintains its O(1) time complexity.

    Here's a breakdown of the time complexity for each part of the Append function:

        if checks: O(1)
        Appending nodes: O(1)
        Updating count: O(1)
        foreach loop:
            Dictionary lookup: O(1) per iteration
            Adding to nodeDictionary: O(1) per iteration
        Overall: O(1)

    In essence, the foreach loop doesn't negate the O(1) complexity because the operations within the loop are themselves constant time, making the overall time complexity independent of the number of elements in the appended list.*/
    // Append the other list to the end of this list, O(1)
    public void Append(DoublyLinkedList<T> other)
    {
        if (other == null || other.Head == null)
        {
            return;
        }

        if (Head == null)
        {
            Head = other.Head;
        }
        else
        {
            Tail.Next = other.Head;
            other.Head.Prev = Tail;
        }

        Tail = other.Tail;
        Count += other.Count;

        // Update nodeDictionary in O(1)
        foreach (var kvp in other.nodeDictionary)
        {
            nodeDictionary[kvp.Key] = kvp.Value;
        }
    }

    // O(1)
    public bool Contains(T value)
    {
        return valueSet.Contains(value); // O(1) time complexity for HashSet contains check
    }

       public IEnumerator<T> GetEnumerator()
    {
        Node<T> current = Head;
        while (current != null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
