using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    // The image of the card
    public Image image;
    // The parent of the card after it has been dragged
    [HideInInspector] public Transform parentAfterDrag;
    [HideInInspector] public Transform parentBeforeDrag;
    private GameController gameController;
    // The original position of the card
    private Vector3 originalPosition;
    
    // Multi-card drag support
    private List<Card> draggedCards = new List<Card>();
    private List<Transform> originalParents = new List<Transform>();
    private List<Vector3> cardOffsets = new List<Vector3>();
    private bool isMultiDrag = false;

    private void Start()
    {
        gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (gameController.GetCurrentPlayer().GetPlayerType().Equals(PlayerType.Human))
        {
            parentBeforeDrag = transform.parent;
            parentAfterDrag = transform.parent;
            
            // Check if this card is in the player grid (hand)
            if (parentBeforeDrag.parent != null && parentBeforeDrag.parent.CompareTag("PlayerGrid"))
            {
                // Try to find adjacent cards that form a valid set
                FindAdjacentSetCards();
            }
            else
            {
                // Single card drag (from board or other)
                draggedCards.Clear();
                draggedCards.Add(GetComponent<Card>());
                isMultiDrag = false;
            }
            
            // Move all dragged cards to root canvas
            for (int i = 0; i < draggedCards.Count; i++)
            {
                Card card = draggedCards[i];
                if (card == null) continue;
                
                originalParents.Add(card.transform.parent);
                card.transform.SetParent(transform.root);
                card.transform.SetAsLastSibling();
                
                // Disable raycast on all dragged cards
                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null) cardImage.raycastTarget = false;
            }
            
            // Calculate offsets for multi-drag visual
            if (isMultiDrag && draggedCards.Count > 0)
            {
                cardOffsets.Clear();
                Vector3 basePos = draggedCards[0].transform.position;
                foreach (Card card in draggedCards)
                {
                    cardOffsets.Add(card.transform.position - basePos);
                }
            }
            
            image.raycastTarget = false;
        }
    }
    
    /// <summary>
    /// Find adjacent cards in the player hand that form a valid run or group
    /// </summary>
    private void FindAdjacentSetCards()
    {
        draggedCards.Clear();
        originalParents.Clear();
        cardOffsets.Clear();
        isMultiDrag = false;
        
        Card thisCard = GetComponent<Card>();
        if (thisCard == null)
        {
            draggedCards.Add(thisCard);
            return;
        }
        
        // Get the player grid
        Transform playerGrid = parentBeforeDrag.parent;
        int thisSlotIndex = parentBeforeDrag.GetSiblingIndex();
        
        // Find all cards to the right that continue a potential set
        List<Card> potentialSet = new List<Card> { thisCard };
        List<Transform> potentialParents = new List<Transform> { parentBeforeDrag };
        
        // Look at cards to the right
        for (int i = thisSlotIndex + 1; i < playerGrid.childCount && potentialSet.Count < 13; i++)
        {
            Transform slot = playerGrid.GetChild(i);
            if (slot.childCount == 0) break; // Empty slot = end of sequence
            
            Card nextCard = slot.GetChild(0).GetComponent<Card>();
            if (nextCard == null) break;
            
            // Check if this card continues a valid set
            if (CanExtendSet(potentialSet, nextCard))
            {
                potentialSet.Add(nextCard);
                potentialParents.Add(slot);
            }
            else
            {
                break; // Card doesn't fit, stop looking
            }
        }
        
        // Only do multi-drag if we have 3+ cards that form a valid set
        if (potentialSet.Count >= 3 && IsValidSet(potentialSet))
        {
            draggedCards = potentialSet;
            originalParents = potentialParents;
            isMultiDrag = true;
            Debug.Log($"<color=green>Multi-drag: {draggedCards.Count} cards forming valid set</color>");
        }
        else
        {
            // Single card drag
            draggedCards.Clear();
            draggedCards.Add(thisCard);
            originalParents.Clear();
            originalParents.Add(parentBeforeDrag);
            isMultiDrag = false;
        }
    }
    
    /// <summary>
    /// Check if a card can extend the current potential set (run or group)
    /// </summary>
    private bool CanExtendSet(List<Card> currentSet, Card newCard)
    {
        if (currentSet.Count == 0) return true;
        
        Card lastCard = currentSet[currentSet.Count - 1];
        Card firstCard = currentSet[0];
        
        // Handle jokers
        if (newCard.Number == Constants.JokerRank) return true;
        if (lastCard.Number == Constants.JokerRank)
        {
            // After joker, accept anything same color (run) or same number (group)
            return newCard.Color == firstCard.Color || newCard.Number == firstCard.Number;
        }
        
        // Check for run (same color, consecutive numbers)
        if (newCard.Color == lastCard.Color && newCard.Number == lastCard.Number + 1)
        {
            // Verify it's consistent with a run pattern
            return IsConsistentRun(currentSet, newCard);
        }
        
        // Check for group (same number, different colors)
        if (newCard.Number == lastCard.Number && newCard.Color != lastCard.Color)
        {
            // Verify no duplicate colors
            foreach (Card c in currentSet)
            {
                if (c.Number != Constants.JokerRank && c.Color == newCard.Color)
                    return false;
            }
            return currentSet.Count < 4; // Max 4 in a group
        }
        
        return false;
    }
    
    private bool IsConsistentRun(List<Card> currentSet, Card newCard)
    {
        // All non-joker cards should have same color
        foreach (Card c in currentSet)
        {
            if (c.Number != Constants.JokerRank && c.Color != newCard.Color)
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// Check if a set of cards forms a valid Rummikub set
    /// </summary>
    private bool IsValidSet(List<Card> cards)
    {
        if (cards.Count < 3) return false;
        
        // Create temporary CardsSet and check validity
        CardsSet testSet = new CardsSet();
        foreach (Card card in cards)
        {
            testSet.AddCardToEnd(card);
        }
        
        return testSet.IsRun() || testSet.IsGroupOfColors();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (gameController.GetCurrentPlayer().GetPlayerType().Equals(PlayerType.Human))
        {
            // Move main card
            transform.position = Input.mousePosition;
            
            // Move attached cards with offsets
            if (isMultiDrag && draggedCards.Count > 1)
            {
                for (int i = 1; i < draggedCards.Count; i++)
                {
                    if (draggedCards[i] != null && i < cardOffsets.Count)
                    {
                        draggedCards[i].transform.position = Input.mousePosition + cardOffsets[i];
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (gameController.GetCurrentPlayer().GetPlayerType().Equals(PlayerType.Human))
        {
            // Re-enable raycast
            image.raycastTarget = true;
            
            if (isMultiDrag)
            {
                // Handle multi-card drop
                HandleMultiCardDrop();
            }
            else
            {
                // Single card drop
                transform.SetParent(parentAfterDrag);
                
                // Re-enable raycast for all cards
                foreach (Card card in draggedCards)
                {
                    if (card != null)
                    {
                        Image cardImage = card.GetComponent<Image>();
                        if (cardImage != null) cardImage.raycastTarget = true;
                    }
                }
            }
            
            // Reset state
            draggedCards.Clear();
            originalParents.Clear();
            cardOffsets.Clear();
            isMultiDrag = false;
        }
    }
    
    /// <summary>
    /// Handle dropping multiple cards on the board
    /// </summary>
    private void HandleMultiCardDrop()
    {
        // Check if dropped on a valid board position
        bool validDrop = false;
        
        if (parentAfterDrag != null && parentAfterDrag.parent != null && 
            parentAfterDrag.parent.CompareTag("BoardGrid"))
        {
            // Check if there's enough space for all cards
            int dropSlotIndex = parentAfterDrag.GetSiblingIndex();
            Transform boardGrid = parentAfterDrag.parent;
            
            // Verify all slots to the right are empty
            bool hasSpace = true;
            for (int i = 0; i < draggedCards.Count; i++)
            {
                int targetIndex = dropSlotIndex + i;
                if (targetIndex >= boardGrid.childCount)
                {
                    hasSpace = false;
                    break;
                }
                
                Transform targetSlot = boardGrid.GetChild(targetIndex);
                if (targetSlot.childCount > 0)
                {
                    hasSpace = false;
                    break;
                }
            }
            
            if (hasSpace)
            {
                validDrop = true;
                
                // Place all cards
                GameBoard board = GameObject.FindGameObjectWithTag("BoardGrid").GetComponent<GameBoard>();
                
                for (int i = 0; i < draggedCards.Count; i++)
                {
                    Card card = draggedCards[i];
                    if (card == null) continue;
                    
                    int targetIndex = dropSlotIndex + i;
                    Transform targetSlot = boardGrid.GetChild(targetIndex);
                    
                    // Set parent to the slot
                    card.transform.SetParent(targetSlot);
                    card.transform.localPosition = Vector3.zero;
                    
                    // Update card position
                    int row = targetIndex / Constants.MaxBoardColumns;
                    int col = targetIndex % Constants.MaxBoardColumns;
                    card.Position = new CardPosition(row, col);
                    
                    // Mark as coming from hand and register the move
                    card.CameFromPlayerHand = true;
                    board.MoveCardFromPlayerHandToGameBoard(card, RemoveOption.Remove);
                    board.AddCardToMovesStack(card);
                    
                    // Re-enable raycast
                    Image cardImage = card.GetComponent<Image>();
                    if (cardImage != null) cardImage.raycastTarget = true;
                }
                
                Debug.Log($"<color=green>Multi-drop successful: {draggedCards.Count} cards placed</color>");
            }
        }
        
        // If not valid, return all cards to original positions
        if (!validDrop)
        {
            Debug.Log("<color=yellow>Multi-drop failed - returning cards to hand</color>");
            ReturnCardsToOriginalPositions();
        }
    }
    
    /// <summary>
    /// Return all dragged cards to their original positions in the hand
    /// </summary>
    private void ReturnCardsToOriginalPositions()
    {
        for (int i = 0; i < draggedCards.Count && i < originalParents.Count; i++)
        {
            Card card = draggedCards[i];
            Transform origParent = originalParents[i];
            
            if (card != null && origParent != null)
            {
                card.transform.SetParent(origParent);
                card.transform.localPosition = Vector3.zero;
                
                // Re-enable raycast
                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null) cardImage.raycastTarget = true;
            }
        }
    }
    
    // Getter for multi-drag state (used by TileSlot)
    public bool IsMultiDrag => isMultiDrag;
    public List<Card> GetDraggedCards() => draggedCards;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (gameController.GetCurrentPlayer().GetPlayerType().Equals(PlayerType.Human))
        {
            // Store the original position of the card
            originalPosition = image.transform.position;
            // Move the card up by a certain amount
            image.transform.position = new Vector3(image.transform.position.x, image.transform.position.y + 6, image.transform.position.z);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (gameController.GetCurrentPlayer().GetPlayerType().Equals(PlayerType.Human))
        {
            // Move the card back to its original position
            image.transform.position = originalPosition;
        }
    }
}
