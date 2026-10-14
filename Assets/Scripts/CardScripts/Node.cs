// Handles expose traversal but cannot publicly mutate the list topology or value.
public class Node<T>
{
    public T Value { get; private set; }
    public Node<T> Next { get; internal set; }
    public Node<T> Prev { get; internal set; }
    internal DoublyLinkedList<T> Owner { get; set; }
    public Node() { }
    internal Node(T value, DoublyLinkedList<T> owner) { Value = value; Owner = owner; }
}
