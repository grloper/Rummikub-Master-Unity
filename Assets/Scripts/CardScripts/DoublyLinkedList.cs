using System.Collections;
using System.Collections.Generic;

// Values may repeat. Hash equality must remain stable while a value is in the list.
public class DoublyLinkedList<T> : IEnumerable<T>
{
    public Node<T> Head { get; private set; }
    public Node<T> Tail { get; private set; }
    public int Count { get; private set; }
    private Dictionary<T, int> valueCounts = new Dictionary<T, int>();
    private int nullCount;

    private void IndexAdd(T value)
    {
        if (value is null) { nullCount++; return; }
        valueCounts.TryGetValue(value, out int count);
        valueCounts[value] = count + 1;
    }
    private void IndexRemove(T value)
    {
        if (value is null) { nullCount--; return; }
        int count = valueCounts[value];
        if (count == 1) valueCounts.Remove(value);
        else valueCounts[value] = count - 1;
    }
    // Expected amortized O(1), including hash-index maintenance.
    public void AddFirst(T value)
    {
        var node = new Node<T>(value, this) { Next = Head };
        if (Head == null) Tail = node;
        else Head.Prev = node;
        Head = node;
        Count++;
        IndexAdd(value);
    }
    public void AddLast(T value)
    {
        var node = new Node<T>(value, this) { Prev = Tail };
        if (Tail == null) Head = node;
        else Tail.Next = node;
        Tail = node;
        Count++;
        IndexAdd(value);
    }
    public Node<T> GetFirstNode() => Head;
    public Node<T> GetLastNode() => Tail;
    public void RemoveFirst() => Remove(Head);
    public void RemoveLast() => Remove(Tail);

    // A node handle is removable only by its current owner, exactly once.
    public void Remove(Node<T> node)
    {
        if (node == null || node.Owner != this) return;
        if (node.Prev == null) Head = node.Next;
        else node.Prev.Next = node.Next;
        if (node.Next == null) Tail = node.Prev;
        else node.Next.Prev = node.Prev;
        Count--;
        IndexRemove(node.Value);
        node.Prev = node.Next = null;
        node.Owner = null;
    }

    // Move nodes, consuming other. O(m) expected for m appended nodes:
    // pointer splice is O(1); ownership and membership maintenance are O(m).
    public void Append(DoublyLinkedList<T> other)
    {
        if (other == null || ReferenceEquals(other, this) || other.Count == 0) return;
        for (var node = other.Head; node != null; node = node.Next)
        {
            node.Owner = this;
            IndexAdd(node.Value);
        }
        if (Tail == null) Head = other.Head;
        else { Tail.Next = other.Head; other.Head.Prev = Tail; }
        Tail = other.Tail;
        Count += other.Count;
        other.Head = other.Tail = null;
        other.Count = other.nullCount = 0;
        // Replace rather than scan a possibly oversized historical hash table.
        other.valueCounts = new Dictionary<T, int>();
    }
    // Expected O(1); hash collisions can make worst-case lookup linear.
    public bool Contains(T value) => value is null ? nullCount > 0 : valueCounts.ContainsKey(value);
    public IEnumerator<T> GetEnumerator()
    {
        for (var node = Head; node != null; node = node.Next) yield return node.Value;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
