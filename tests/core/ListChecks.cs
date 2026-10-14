static class ListChecks {
    public static void Run(Action<bool, string> check) {
        var list = new DoublyLinkedList<string>();
        list.AddLast("same"); list.AddFirst("same"); list.RemoveFirst();
        check(list.Contains("same") && list.Count == 1, "duplicate value retains membership");
        list.AddLast(null); list.AddFirst(null); list.RemoveFirst();
        check(list.Contains(null), "duplicate null retains membership");
        var detached = list.Head; list.Remove(detached); list.Remove(detached);
        check(list.Count == 1 && detached.Next == null && detached.Prev == null, "removed handle detaches; repeated remove harmless");
        var donor = new DoublyLinkedList<string>(); donor.AddLast("a"); donor.AddLast("b");
        var moved = donor.Head;
        list.Remove(moved);
        check(list.Count == 1 && donor.Count == 2, "foreign removal harmless");
        list.Append(list);
        check(list.Count == 1 && list.ToArray().Length == 1, "self append harmless");
        list.Append(donor);
        check(list.SequenceEqual(new string[] { null, "a", "b" }) && donor.Count == 0 && donor.Head == null && donor.Tail == null && !donor.Contains("a"), "append transfers and clears donor");
        donor.Remove(moved); donor.AddLast("new"); list.Remove(moved);
        check(list.SequenceEqual(new string[] { null, "b" }) && donor.SequenceEqual(new[] { "new" }), "transferred handle belongs only to receiver; donor reusable");
        var random = new Random(81731); var actual = new DoublyLinkedList<int>(); var model = new List<int>();
        for (int i = 0; i < 3000; i++) {
            int value = random.Next(8);
            switch(random.Next(6)) {
                case 0: actual.AddFirst(value); model.Insert(0, value); break;
                case 1: actual.AddLast(value); model.Add(value); break;
                case 2: actual.RemoveFirst(); if(model.Count > 0) model.RemoveAt(0); break;
                case 3: actual.RemoveLast(); if(model.Count > 0) model.RemoveAt(model.Count - 1); break;
                case 4:
                    if(model.Count > 0) {
                        int index = random.Next(model.Count); var node = actual.Head;
                        for(int j=0;j<index;j++) node=node.Next;
                        actual.Remove(node); actual.Remove(node); model.RemoveAt(index);
                    } break;
                case 5:
                    var source = new DoublyLinkedList<int>(); source.AddLast(value); source.AddLast(value);
                    actual.Append(source); model.Add(value); model.Add(value); break;
            }
            check(actual.Count == model.Count && actual.SequenceEqual(model), "randomized list/model order and count");
            for(int v=0;v<8;v++) check(actual.Contains(v) == model.Contains(v), "randomized multiset membership");
            var reverse = new List<int>();
            for(var node=actual.Tail;node!=null;node=node.Prev) {
                check(node.Next == null ? node == actual.Tail : node.Next.Prev == node, "bidirectional link consistency");
                reverse.Add(node.Value);
                if(reverse.Count > actual.Count) throw new Exception("cycle");
            }
            check(reverse.SequenceEqual(model.AsEnumerable().Reverse()), "reverse traversal");
        }
    }
}
