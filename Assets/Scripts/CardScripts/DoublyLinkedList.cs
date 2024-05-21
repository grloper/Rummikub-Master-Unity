using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoublyLinkedList<T> : IEnumerable<T>
{
    public Node<T> Head { get; private set; } // head node
    public Node<T> Tail { get; private set; } // tail node
    public int Count { get; private set; } // number of elements in the list
    private readonly HashSet<T> valueSet; // HashSet for constant time contains checks
    // O(1)
    // Constructor with no parameters, initializes the linked list
    public DoublyLinkedList()
    {
        Count = 0;
        valueSet = new HashSet<T>();
    }
    // O(1)
    public void AddFirst(T value)
    {
        Node<T> node = new Node<T> { Value = value }; // Create a new node with the given value
        if (Head == null) // If the list is empty
        {
            Head = Tail = node; // Set the head and tail to the new node
        }
        else // If the list is not empty
        {
            // Adjust the pointers to insert the new node at the beginning
            node.Next = Head;
            Head.Prev = node;
            Head = node;
        }
        Count++; // Increment the count
        valueSet.Add(value); // Add value to HashSet
    }
    // O(1)
    public void AddLast(T value)
    {
        Node<T> node = new Node<T> { Value = value }; // Create a new node with the given value
        if (Tail == null) // If the list is empty
        {
            Head = Tail = node; // Set the head and tail to the new node
        }
        else // If the list is not empty
        {
            // Adjust the pointers to insert the new node at the end
            node.Prev = Tail;
            Tail.Next = node;
            Tail = node;
        }

        Count++; // Increment the count
        valueSet.Add(value); // Add value to HashSet
    }
    // O(1)
    public Node<T> GetFirstNode()
    {
        return Head; // Return the head node
    }
    // O(1)
    public Node<T> GetLastNode()
    {
        return Tail; // Return the tail node
    }
    // Remove the first node in the linked list, O(1)
    public void RemoveFirst()
    {
        if (Head == null) // If the list is empty
        {
            return; // Nothing to remove if the list is empty
        }

        if (Head == Tail) // If there's only one element in the list
        {
            Head = Tail = null; // Set the head and tail to null
        }
        else // If there are more than one elements in the list
        {
            Head = Head.Next; // Move the head to the next node
            if (Head != null) // If the head is not null
            {
                Head.Prev = null; // Set the previous pointer of the new head to null
            }
        }
        Count = (Head == null) ? 0 : Count - 1; // Adjust count accordingly, if Head is null, set count to 0 else decrement count by 1

        // Remove the value from the HashSet if Head is not null
        if (Head != null)
        {
            valueSet.Remove(Head.Value); // Remove the value from the HashSet, O(1)
        }
    }
    // Remove the last node in the linked list, O(1)
    public void RemoveLast()
    {
        if (Tail == null) // If the list is empty
        {
            return; // Nothing to remove if the list is empty
        }

        if (Head == Tail) // If there is only one element in the list
        {
            Head = Tail = null; // Set the head and tail to null
        }
        else // If there are more than one elements in the list
        {
            // Move the tail to the previous node
            Tail = Tail.Prev;
            if (Tail != null) // If the tail is not null (more than one element in the list)
            {
                Tail.Next = null; // Set the next pointer of the new tail to null
            }
        }
        Count = (Tail == null) ? 0 : Count - 1; // Adjust count accordingly, if Tail is null, set count to 0 else decrement count by 1

        // Remove the value from the HashSet if Tail is not null
        if (Tail != null)
        {
            valueSet.Remove(Tail.Value); // Remove the value from the HashSet, O(1)
        }
    }

    // Remove a node from the linked list, O(1)
    public void Remove(Node<T> node)
    {
        if (node == null) // If the node is null
        {
            return; // Nothing to remove if the node is null
        }

        if (node == Head) // If the node is the head
        {
            RemoveFirst();
        }
        else if (node == Tail) // If the node is the tail
        {
            RemoveLast();
        }
        else // If the node is neither the head nor the tail
        {
            // Adjust the pointers to skip over the 'node'
            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;
            Count--;
            // Remove the value from the HashSet
            valueSet.Remove(node.Value);
        }
    }


    // O(1), best function for the rummikub's data structure
    public void Append(DoublyLinkedList<T> other)
    {
        if (other == null || other.Head == null) // If the other list is null or empty, return
        {
            return;
        }
        if (Head == null) // If the current list is empty, set the head and tail to the other list's head and tail
        {
            Head = other.Head; // Set the head to the other list's head
        }
        else // If the current list is not empty
        {
            // Adjust the pointers to append the other list to the current list
            Tail.Next = other.Head;
            other.Head.Prev = Tail;
        }
        // Set the tail to the other list's tail and increment the count
        Tail = other.Tail;
        Count += other.Count;
    }
    // O(1)
    public bool Contains(T value)
    {
        return valueSet.Contains(value); // O(1) time complexity for HashSet contains check
    }
    // O(n) where n is the number of elements in the list (Count)
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
