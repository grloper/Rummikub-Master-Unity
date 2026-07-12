
public interface ICardSet
{
    bool IsRun();
    bool IsGroupOfColors();
    bool IsContainsCard(Card card);
    Card GetFirstCard();
    Card GetLastCard();
    void AddCardToBeginning(Card card);
    void AddCardToEnd(Card card);
    bool IsSameColor(Card c1, CardColor color);
    bool IsConsecutive(Card c1, Card c2);


}
