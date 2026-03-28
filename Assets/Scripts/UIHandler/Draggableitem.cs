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
    
    // Animation settings
    private const float LIFT_HEIGHT = 6f;
    private const float LIFT_DELAY = 0.08f; // Delay between each card lift
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Card> liftedCards = new List<Card>();
    private Coroutine liftCoroutine;
    private bool isLifting = false;

    private void Start()
    {
        gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (gameController.GetCurrentPlayer().GetPlayerType().Equals(PlayerType.Human))
        {
            // Stop any ongoing lift animation
            if (liftCoroutine != null)
            {
                StopCoroutine(liftCoroutine);
                liftCoroutine = null;
            }
            
            parentBeforeDrag = transform.parent;
            parentAfterDrag = transform.parent;
            
            // Check if this card is in the player grid (hand)
            if (parentBeforeDrag.parent != null && parentBeforeDrag.parent.CompareTag("PlayerGrid"))
            {
                // Find adjacent cards (2+ for partial sets)
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
            originalParents.Clear();
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
    /// Find adjacent cards in the player hand that form a partial or valid set (2+ cards)
    /// </summary>
    private void FindAdjacentSetCards()
    {
        draggedCards.Clear();
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
            
            // Check if this card continues a valid set pattern
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
        
        // Multi-drag if we have 2+ cards forming a partial set
        if (potentialSet.Count >= 2)
        {
            draggedCards = new List<Card>(potentialSet);
            originalParents = new List<Transform>(potentialParents);
            isMultiDrag = true;
            Debug.Log($"<color=cyan>Multi-drag: {draggedCards.Count} cards (partial/full set)</color>");
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
        
        // Handle jokers - they can extend anything
        if (newCard.Number == Constants.JokerRank) return true;
        if (lastCard.Number == Constants.JokerRank)
        {
            // After joker, accept anything same color (run) or same number (group)
            // Find first non-joker card to determine pattern
            Card patternCard = null;
            foreach (Card c in currentSet)
            {
                if (c.Number != Constants.JokerRank)
                {
                    patternCard = c;
                    break;
                }
            }
            if (patternCard == null) return true; // All jokers so far
            return newCard.Color == patternCard.Color || newCard.Number == patternCard.Number;
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
    /// Check if a set of cards forms a valid Rummikub set (3+ cards)
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
            liftedCards.Clear();
            originalPositions.Clear();
        }
    }
    
    /// <summary>
    /// Handle dropping multiple cards on the board or hand
    /// </summary>
    private void HandleMultiCardDrop()
    {
        bool validDrop = false;
        
        // Dropping on board
        if (parentAfterDrag != null && parentAfterDrag.parent != null && 
            parentAfterDrag.parent.CompareTag("BoardGrid"))
        {
            // Only allow board drop if it's a valid 3+ set
            if (draggedCards.Count >= 3 && IsValidSet(draggedCards))
            {
                validDrop = TryPlaceOnBoard();
            }
            else
            {
                Debug.Log("<color=yellow>Cannot drop partial set on board - need 3+ valid cards</color>");
            }
        }
        // Dropping on player hand (rearranging)
        else if (parentAfterDrag != null && parentAfterDrag.parent != null && 
                 parentAfterDrag.parent.CompareTag("PlayerGrid"))
        {
            validDrop = TryPlaceInHand();
        }
        
        // If not valid, return all cards to original positions
        if (!validDrop)
        {
            Debug.Log("<color=yellow>Multi-drop failed - returning cards</color>");
            ReturnCardsToOriginalPositions();
        }
    }
    
    /// <summary>
    /// Try to place cards on the board, finding space if needed
    /// </summary>
    private bool TryPlaceOnBoard()
    {
        Transform boardGrid = parentAfterDrag.parent;
        int dropSlotIndex = parentAfterDrag.GetSiblingIndex();
        
        // First try the exact drop location
        int? foundIndex = FindSpaceForCards(boardGrid, dropSlotIndex, draggedCards.Count);
        
        // If not found, search nearby
        if (foundIndex == null)
        {
            foundIndex = FindNearestSpace(boardGrid, dropSlotIndex, draggedCards.Count);
        }
        
        if (foundIndex == null)
        {
            Debug.Log("<color=red>No space found on board</color>");
            return false;
        }
        
        // Place all cards
        GameBoard board = GameObject.FindGameObjectWithTag("BoardGrid").GetComponent<GameBoard>();
        
        for (int i = 0; i < draggedCards.Count; i++)
        {
            Card card = draggedCards[i];
            if (card == null) continue;
            
            int targetIndex = foundIndex.Value + i;
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
        
        Debug.Log($"<color=green>Multi-drop successful: {draggedCards.Count} cards placed at index {foundIndex}</color>");
        return true;
    }
    
    /// <summary>
    /// Find space for N consecutive cards starting at or near startIndex
    /// </summary>
    private int? FindSpaceForCards(Transform grid, int startIndex, int count)
    {
        // Check if all slots from startIndex are empty
        bool hasSpace = true;
        for (int i = 0; i < count; i++)
        {
            int idx = startIndex + i;
            if (idx >= grid.childCount || grid.GetChild(idx).childCount > 0)
            {
                hasSpace = false;
                break;
            }
        }
        
        return hasSpace ? startIndex : (int?)null;
    }
    
    /// <summary>
    /// Find nearest space that can fit all cards
    /// </summary>
    private int? FindNearestSpace(Transform grid, int preferredIndex, int count)
    {
        int maxColumns = Constants.MaxBoardColumns;
        int preferredRow = preferredIndex / maxColumns;
        
        // Search expanding outward from preferred position
        for (int distance = 1; distance < grid.childCount; distance++)
        {
            // Try same row first (left then right)
            int leftIdx = preferredIndex - distance;
            int rightIdx = preferredIndex + distance;
            
            // Check left
            if (leftIdx >= 0 && leftIdx / maxColumns == preferredRow)
            {
                int? found = FindSpaceForCards(grid, leftIdx, count);
                if (found != null) return found;
            }
            
            // Check right
            if (rightIdx < grid.childCount && rightIdx / maxColumns == preferredRow)
            {
                int? found = FindSpaceForCards(grid, rightIdx, count);
                if (found != null) return found;
            }
        }
        
        // If same row didn't work, search other rows
        for (int row = 0; row < Constants.MaxBoardRows; row++)
        {
            if (row == preferredRow) continue;
            
            for (int col = 0; col <= maxColumns - count; col++)
            {
                int idx = row * maxColumns + col;
                int? found = FindSpaceForCards(grid, idx, count);
                if (found != null) return found;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Try to place cards in the player hand (rearranging)
    /// </summary>
    private bool TryPlaceInHand()
    {
        Transform playerGrid = parentAfterDrag.parent;
        int dropSlotIndex = parentAfterDrag.GetSiblingIndex();
        
        // Find space in hand
        int? foundIndex = FindSpaceForCards(playerGrid, dropSlotIndex, draggedCards.Count);
        
        if (foundIndex == null)
        {
            foundIndex = FindNearestSpaceInHand(playerGrid, dropSlotIndex, draggedCards.Count);
        }
        
        if (foundIndex == null)
        {
            return false;
        }
        
        // Place cards in hand
        for (int i = 0; i < draggedCards.Count; i++)
        {
            Card card = draggedCards[i];
            if (card == null) continue;
            
            int targetIndex = foundIndex.Value + i;
            Transform targetSlot = playerGrid.GetChild(targetIndex);
            
            card.transform.SetParent(targetSlot);
            card.transform.localPosition = Vector3.zero;
            
            // Update card position
            int row = targetIndex / Constants.MaxPlayerColumns;
            int col = targetIndex % Constants.MaxPlayerColumns;
            card.Position = new CardPosition(row, col);
            
            // Re-enable raycast
            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null) cardImage.raycastTarget = true;
        }
        
        Debug.Log($"<color=cyan>Cards rearranged in hand at index {foundIndex}</color>");
        return true;
    }
    
    /// <summary>
    /// Find nearest space in player hand
    /// </summary>
    private int? FindNearestSpaceInHand(Transform grid, int preferredIndex, int count)
    {
        int maxColumns = Constants.MaxPlayerColumns;
        
        // Search expanding outward
        for (int distance = 1; distance < grid.childCount; distance++)
        {
            int leftIdx = preferredIndex - distance;
            int rightIdx = preferredIndex + distance;
            
            if (leftIdx >= 0)
            {
                int? found = FindSpaceForCards(grid, leftIdx, count);
                if (found != null) return found;
            }
            
            if (rightIdx < grid.childCount)
            {
                int? found = FindSpaceForCards(grid, rightIdx, count);
                if (found != null) return found;
            }
        }
        
        return null;
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
            image.transform.position = new Vector3(image.transform.position.x, image.transform.position.y + LIFT_HEIGHT, image.transform.position.z);
            
            // Start cascading lift animation for adjacent cards
            liftCoroutine = StartCoroutine(LiftAdjacentCardsAnimated());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (gameController.GetCurrentPlayer().GetPlayerType().Equals(PlayerType.Human))
        {
            // Stop lift animation if still running
            if (liftCoroutine != null)
            {
                StopCoroutine(liftCoroutine);
                liftCoroutine = null;
            }
            
            // Move the card back to its original position
            image.transform.position = originalPosition;
            
            // Lower all lifted cards back with animation
            StartCoroutine(LowerAdjacentCardsAnimated());
        }
    }
    
    /// <summary>
    /// Lift adjacent cards one by one with a cascading animation
    /// </summary>
    private IEnumerator LiftAdjacentCardsAnimated()
    {
        isLifting = true;
        originalPositions.Clear();
        liftedCards.Clear();
        
        Transform parent = transform.parent;
        if (parent == null || parent.parent == null || !parent.parent.CompareTag("PlayerGrid"))
        {
            isLifting = false;
            yield break;
        }
        
        Card thisCard = GetComponent<Card>();
        if (thisCard == null)
        {
            isLifting = false;
            yield break;
        }
        
        Transform playerGrid = parent.parent;
        int thisSlotIndex = parent.GetSiblingIndex();
        
        // Build potential set (2+ for visual preview)
        List<Card> potentialSet = new List<Card> { thisCard };
        
        for (int i = thisSlotIndex + 1; i < playerGrid.childCount && potentialSet.Count < 13; i++)
        {
            Transform slot = playerGrid.GetChild(i);
            if (slot.childCount == 0) break;
            
            Card nextCard = slot.GetChild(0).GetComponent<Card>();
            if (nextCard == null) break;
            
            if (CanExtendSet(potentialSet, nextCard))
            {
                potentialSet.Add(nextCard);
            }
            else
            {
                break;
            }
        }
        
        // Lift cards one by one with delay (skip first card, already lifted)
        if (potentialSet.Count >= 2)
        {
            for (int i = 1; i < potentialSet.Count; i++)
            {
                if (!isLifting) yield break; // Check if we should stop
                
                Card card = potentialSet[i];
                Vector3 origPos = card.transform.position;
                originalPositions.Add(origPos);
                liftedCards.Add(card);
                
                // Animate lift
                yield return StartCoroutine(AnimateLift(card, origPos, LIFT_HEIGHT, 0.05f));
                
                // Small delay before next card
                yield return new WaitForSeconds(LIFT_DELAY);
            }
        }
        
        isLifting = false;
    }
    
    /// <summary>
    /// Animate a single card lifting up
    /// </summary>
    private IEnumerator AnimateLift(Card card, Vector3 startPos, float height, float duration)
    {
        Vector3 endPos = new Vector3(startPos.x, startPos.y + height, startPos.z);
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            card.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        card.transform.position = endPos;
    }
    
    /// <summary>
    /// Lower all lifted cards back with cascading animation
    /// </summary>
    private IEnumerator LowerAdjacentCardsAnimated()
    {
        isLifting = false;
        
        // Lower cards in reverse order for nice effect
        for (int i = liftedCards.Count - 1; i >= 0; i--)
        {
            if (i < liftedCards.Count && i < originalPositions.Count)
            {
                Card card = liftedCards[i];
                Vector3 targetPos = originalPositions[i];
                
                if (card != null)
                {
                    // Quick animate down
                    yield return StartCoroutine(AnimateLower(card, targetPos, 0.03f));
                }
            }
        }
        
        liftedCards.Clear();
        originalPositions.Clear();
    }
    
    /// <summary>
    /// Animate a single card lowering down
    /// </summary>
    private IEnumerator AnimateLower(Card card, Vector3 targetPos, float duration)
    {
        Vector3 startPos = card.transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            card.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        card.transform.position = targetPos;
    }
}
