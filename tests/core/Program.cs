static class Program {
    static int checks;
    static void Check(bool value, string label) {
        if (!value) throw new Exception(label);
        checks++;
    }
    static CardsSet Set(params Card[] cards) {
        var set = new CardsSet();
        foreach (var card in cards) set.AddCardToEnd(card);
        return set;
    }
    static Card C(int n, CardColor color = CardColor.Red) => new(n, color);
    static void Main(string[] args) {
        if (args.Contains("--measure")) { StructureMeasurements.Run(); return; }
        ListChecks.Run(Check);
        Check(Set(C(1), C(2), C(3)).IsRun(), "basic run");
        Check(!Set(C(15), C(1), C(2)).IsRun(), "leading joker cannot be zero");
        Check(!Set(C(12), C(13), C(15)).IsRun(), "trailing joker cannot be fourteen");
        Check(Set(C(10), C(15), C(12)).IsRun(), "interior joker");
        Check(!Set(C(1), C(2, CardColor.Blue), C(3)).IsRun(), "mixed-color run");
        Check(!Set(C(7), C(7), C(7, CardColor.Blue)).IsGroupOfColors(), "duplicate-color group");
        Check(Set(C(7), C(15), C(7, CardColor.Blue)).IsGroupOfColors(), "joker group");
        var run = Set(C(3), C(4), C(5));
        Check(run.IsRun(), "run flags");
        Check(run.CanAddCardLast(C(6)), "append probe");
        Check(run.set.Count == 3 && run.isRun && !run.isGroupOfColors, "probe restores state");
        var existing = run.GetFirstCard();
        Check(!run.CanAddCardFirst(existing) && !run.CanAddCardLast(existing), "cannot probe duplicate physical tile");
        Check(run.IsContainsCard(existing) && run.set.Count == 3 && run.IsRun(), "duplicate probe preserves membership index");
        var a = C(1); var b = C(1);
        var list = new DoublyLinkedList<Card>(); list.AddLast(a); list.AddLast(b);
        list.RemoveFirst();
        Check(!list.Contains(a) && list.Contains(b) && list.Count == 1, "physical copies remain distinct");
        var left = Set(C(3), C(4), C(5));
        var right = Set(C(6), C(7), C(8));
        left.Combine(left, right);
        Check(left.IsRun() && left.set.Count == 6 && right.set.Count == 0, "production Combine consumes donor and preserves run");
        var split = left.UnCombine(3);
        Check(split.IsRun() && left.IsRun() && split.set.Count == 3 && left.set.Count == 3, "production UnCombine splits moved nodes into valid runs");
        var deck = new RummikubDeck(); var drawn = new HashSet<Card>();
        while (deck.GetDeckLength() > 0) Check(drawn.Add(deck.DrawRandomCardFromDeck()), "no repeated physical card");
        Check(drawn.Count == 106 && drawn.Count(c => c.Number == 15) == 2, "complete deck");
        try { deck.DrawRandomCardFromDeck(); throw new Exception("empty deck must throw"); }
        catch (EmptyDeckException) { checks++; }
        Console.WriteLine($"PASS: {checks} assertions against linked production C# classes (Unity adapters; not a scene run).");
    }
}
