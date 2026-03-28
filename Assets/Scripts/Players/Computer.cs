using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.Unicode;

public class Computer : Player
{

    // game board reference
    [HideInInspector] private GameBoard gameBoard;
    // Reference to the ui manager 
    [HideInInspector] private UImanager uiManager;
    // Reference to the game controller
    [HideInInspector] private GameController gameController;
    // The delay for the computer move
    public float computerMoveDelay = 0.1f;
    // Player reference
    private Player myPlayer;
    private bool added; // added single cards
    private bool dropped; // dropped valid sets
    private bool partial; // dropped partial sets with free cards
    private bool chainExtracted; // dropped sets via chain extraction
    
    // Chain extractor for advanced AI moves
    private ChainExtractor chainExtractor;

    // O(n) where n is the number of cards in hand, returns the maximum valid run sets
    private List<CardsSet> ExtractMaxValidRunSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        List<CardsSet> cardsSets = new List<CardsSet>(); // returned list of cards sets that are valid
        CardsSet currentSet = new CardsSet(); // current set of cards
        if (list.Count == 0) // if the list is empty return an empty list
            return cardsSets;
        
        // FIX: Create a copy to avoid mutation during iteration
        List<Card> workingList = new List<Card>(list);
        
        // FIX: Move duplicates to end before iterating
        List<Card> duplicates = new List<Card>();
        List<Card> unique = new List<Card>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (i > 0 && workingList[i].Number == workingList[i - 1].Number &&
                workingList[i].Color == workingList[i - 1].Color)
            {
                duplicates.Add(workingList[i]);
            }
            else
            {
                unique.Add(workingList[i]);
            }
        }
        workingList = unique;
        workingList.AddRange(duplicates);
        
        if (workingList.Count == 0) return cardsSets;
        
        currentSet.AddCardToEnd(workingList[0]); // add the first card to the current set - Step A
        for (int i = 1; i < workingList.Count; i++) // iterate over the remaining cards
        {
            if (workingList[i].Number == workingList[i - 1].Number + 1 // on going run logic check
             && workingList[i].Color == workingList[i - 1].Color
             && currentSet.set.Count <= maxRangeInclusive)
            {
                currentSet.AddCardToEnd(workingList[i]); // keep track of the longest run
            }
            else // break happened
            {
                if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive) // if the longest run we found so far, meets the requirements 
                {
                    cardsSets.Add(currentSet); // add the current set to the list of valid sets
                }
                currentSet = new CardsSet(); // otherwise either way reset the count
                currentSet.AddCardToEnd(workingList[i]); // repeat step A
            }
        }
        // FIX: Don't forget to check the last set!
        if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive)
        {
            cardsSets.Add(currentSet);
        }
        return cardsSets;
    }
    // O(n) where n is the number of cards in hand
    private List<CardsSet> ExtractMaxValidGroupSets(List<Card> list, int minRangeInclusive, int maxRangeInclusive)
    {
        List<CardsSet> cardsSets = new List<CardsSet>(); // list of valid sets
        CardsSet currentSet = new CardsSet(); // current set
        if (list.Count == 0) // if the list is empty return an empty list
            return cardsSets;
        
        // FIX: Create a copy to avoid mutation during iteration
        List<Card> workingList = new List<Card>(list);
        
        // FIX: Move duplicates to end before iterating
        List<Card> duplicates = new List<Card>();
        List<Card> unique = new List<Card>();
        for (int i = 0; i < workingList.Count; i++)
        {
            if (i > 0 && workingList[i].Number == workingList[i - 1].Number &&
                workingList[i].Color == workingList[i - 1].Color)
            {
                duplicates.Add(workingList[i]);
            }
            else
            {
                unique.Add(workingList[i]);
            }
        }
        workingList = unique;
        workingList.AddRange(duplicates);
        
        if (workingList.Count == 0) return cardsSets;
        
        currentSet.AddCardToEnd(workingList[0]); // add the first card to the current set, Step A
        for (int i = 1; i < workingList.Count; i++) // iterate over the remaining cards
        {
            if (workingList[i].Number == workingList[i - 1].Number // on going group logic check
                && !currentSet.IsContainThisColor(workingList[i].Color)
                 && currentSet.set.Count <= maxRangeInclusive)
            {
                currentSet.AddCardToEnd(workingList[i]); // keep track of the longest group
            }
            else // break happened (by the way it sorted for max = 4 cards it breaks.)
            {
                if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive) // if the longest group we found so far, meets the requirements
                {
                    cardsSets.Add(currentSet); // add the current set to the list of valid sets
                }
                currentSet = new CardsSet(); // otherwise either way reset the count
                currentSet.AddCardToEnd(workingList[i]);
            }
        }
        // FIX: Don't forget to check the last set!
        if (currentSet.set.Count >= minRangeInclusive && currentSet.set.Count <= maxRangeInclusive)
        {
            cardsSets.Add(currentSet);
        }
        return cardsSets; // return the list of valid sets
    }


    // we want to play all the valids sets that are in hand
    public void MaximizeValidDrops()
    {
        this.dropped = false; // indicate that we haven't dropped a set yet
        List<CardsSet> setsRun = ExtractMaxValidRunSets(myPlayer.GetPlayerHand().SortedByRun(), Constants.MinInRun, Constants.MaxInRun);
        if (setsRun.Count > 0) // if we have valid run sets
        {
            this.dropped = true; // indicate that we dropped a set (for confirmation)
            foreach (CardsSet set in setsRun)
            {
                gameBoard.PlayCardSetOnBoard(set); // play the set on the game board (this method update the data structers and the visuals)
            }
        }
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(myPlayer.GetPlayerHand().SortedByGroup(), Constants.MinInGroup, Constants.MaxInGroup);
        if (setsGroup.Count > 0)
        {
            this.dropped = true; // indicate that we dropped a set (for confirmation)
            foreach (CardsSet set in setsGroup)
            {
                gameBoard.PlayCardSetOnBoard(set); // play the set on the game board (this method update the data structers and the visuals)
            }
        }

    }
    public void Initialize(Player player)
    {
        this.myPlayer = player; // set the player reference
        this.gameController = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameController>(); // get the game controller reference, to call the methods
        this.uiManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<UImanager>(); // get the ui manager reference, to call the methods 
        this.gameBoard = GameObject.FindGameObjectWithTag("BoardGrid").GetComponent<GameBoard>(); // get the game board reference, to call the methods
        this.chainExtractor = new ChainExtractor(gameBoard, player, uiManager); // initialize the chain extractor
    }

    public IEnumerator ComputerMove()
    {
        yield return new WaitForSeconds(computerMoveDelay); // wait for the delay
        // Call the method inside Computer.cs named "DoComputerMove"
        DoComputerMove();

    }

    // O(n*logm) where n is the number of cards in the player hand, and m is the number of sets on the game board
    // Main method for the computer move
    private void DoComputerMove()
    {
        Debug.Log($"<color=cyan>═══════════════════════════════════════════════════</color>");
        Debug.Log($"<color=cyan>COMPUTER TURN START - Hand: {myPlayer.GetPlayerHand().SortedByRun().Count} cards</color>");
        Debug.Log($"<color=cyan>═══════════════════════════════════════════════════</color>");
        
        MaximizeValidDrops(); // maximize the valid drops. Step 1, O(n)
        bool append = false; // flag to indicate if we appended a card to the board (either mamximize partial or assigned free cards to existing sets)
        if (myPlayer.GetInitialMove()) //if the player is allowed to append cards to the board
        {
            int loopCount = 0;
            const int maxLoops = 20; // Safety limit
            
            // perform the following actions until the computer has no moves to make
            do
            {
                loopCount++;
                if (loopCount > maxLoops)
                {
                    Debug.LogWarning($"<color=red>AI loop safety break after {maxLoops} iterations</color>");
                    break;
                }
                
                Debug.Log($"<color=white>--- Loop {loopCount}: partial={partial}, added={added}, chainExtracted={chainExtracted}</color>");
                
                if (partial || added || chainExtracted)
                    append = true;
                MaximizePartialDrops(); // maximize the partial drops. Step 2 O(n*logm)
                AssignFreeCardsToExistsSets(); // assign free cards to existing sets. Step 3 (O(n*logm)
                MaximizeChainExtractions(); // NEW: chain extractions (1 hand + 2 board) Step 4
            }
            while (partial || added || chainExtracted); // do it on repeat till the computer has no moves to make
        }
        else if (gameBoard.GetMovesStackSum() < Constants.MinFirstSet)
        {
            //if the player is not allowed to append cards to the board and the computer has less than 30 points of cards to drop
            Debug.Log($"<color=orange>Drawing card - not enough points for first move ({gameBoard.GetMovesStackSum()}/{Constants.MinFirstSet})</color>");
            uiManager.DrawACardFromDeck();
            PrintEndOfTurnSummary("Drew card (first move threshold)");
            return;
        }
        if (dropped || append) //if the computer has made a move, or have sets to drop.
        {
            Debug.Log($"<color=green>CONFIRMING MOVE - dropped={dropped}, append={append}</color>");
            uiManager.ConfirmMove(); // confirm the move 
            PrintEndOfTurnSummary("Confirmed move");
        }
        else
        {
            Debug.Log($"<color=orange>NO MOVES FOUND - Drawing card</color>");
            uiManager.DrawACardFromDeck(); // draw a card if the computer has no moves to make
            PrintEndOfTurnSummary("Drew card (no moves)");
        }
    }
    
    /// <summary>
    /// Print summary at end of turn for debugging
    /// </summary>
    private void PrintEndOfTurnSummary(string action)
    {
        try
        {
            Debug.Log($"<color=cyan>═══════════════════════════════════════════════════</color>");
            Debug.Log($"<color=cyan>COMPUTER TURN END - {action}</color>");
            Debug.Log($"<color=cyan>Hand remaining: {myPlayer.GetPlayerHand().SortedByRun().Count} cards</color>");
            
            // Print remaining hand
            string handStr = "";
            foreach (Card c in myPlayer.GetPlayerHand())
            {
                if (c != null) handStr += $"{c.Color.ToString()[0]}{c.Number} ";
            }
            Debug.Log($"<color=white>Hand: [{handStr}]</color>");
            
            // Print board state
            var boardSets = gameBoard.board.GetGameBoardValidSetsTable();
            Debug.Log($"<color=white>Board has {boardSets.Count} sets</color>");
            
            Debug.Log($"<color=cyan>═══════════════════════════════════════════════════</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PrintEndOfTurnSummary error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// NEW STRATEGY: Maximize chain extractions
    /// Finds moves where:
    /// - 1 card from hand + 2 cards extracted from board form a valid set
    /// - Joker extraction to complete partials
    /// - Chain moves (add to 3-group to enable extraction)
    /// </summary>
    private void MaximizeChainExtractions()
    {
        this.chainExtracted = false;
        
        try
        {
            // Debug: Print current state
            chainExtractor.PrintDebugState("Before Chain Extractions");
            
            // Strategy 0: Extract jokers to complete partials (highest priority!)
            var jokerPlans = chainExtractor.FindJokerExtractionPlans();
            Debug.Log($"<color=magenta>Found {jokerPlans.Count} joker extraction plans</color>");
            
            foreach (var plan in jokerPlans)
            {
                if (IsPlanStillValid(plan) && chainExtractor.ValidatePlan(plan))
                {
                    Debug.Log($"<color=magenta>JOKER EXTRACTION: {plan}</color>");
                    if (ExecuteJokerPlan(plan))
                    {
                        this.chainExtracted = true;
                        return;
                    }
                }
            }
            
            // Strategy 1: Single card from hand + 2 direct extractions from board
            var directPlans = chainExtractor.FindSingleCardWithDoubleExtraction();
            Debug.Log($"<color=cyan>Found {directPlans.Count} double extraction plans</color>");
            
            // Sort plans by value (prefer higher-value cards to get rid of them)
            directPlans = directPlans.Where(p => p.HandCard != null)
                                      .OrderByDescending(p => p.HandCard.Number)
                                      .ToList();
            
            foreach (var plan in directPlans)
            {
                if (IsPlanStillValid(plan) && chainExtractor.ValidatePlan(plan))
                {
                    Debug.Log($"<color=green>DOUBLE EXTRACTION: {plan}</color>");
                    if (ExecuteChainPlan(plan))
                    {
                        this.chainExtracted = true;
                        return;
                    }
                }
            }
            
            // Strategy 2: Chain extractions (move card A to enable extracting card B)
            var chainPlans = chainExtractor.FindChainExtractionPlans();
            Debug.Log($"<color=cyan>Found {chainPlans.Count} chain extraction plans</color>");
            
            foreach (var plan in chainPlans)
            {
                if (IsPlanStillValid(plan) && chainExtractor.ValidatePlan(plan))
                {
                    Debug.Log($"<color=blue>CHAIN EXTRACTION: {plan}</color>");
                    if (ExecuteChainPlan(plan))
                    {
                        this.chainExtracted = true;
                        return;
                    }
                }
            }
            
            // Strategy 3: Advanced chain plans (complex multi-step extractions)
            var advancedPlans = chainExtractor.FindAdvancedChainPlans();
            Debug.Log($"<color=cyan>Found {advancedPlans.Count} advanced chain plans</color>");
            
            foreach (var plan in advancedPlans)
            {
                if (IsPlanStillValid(plan) && chainExtractor.ValidatePlan(plan))
                {
                    Debug.Log($"<color=yellow>ADVANCED CHAIN: {plan}</color>");
                    if (ExecuteChainPlan(plan))
                    {
                        this.chainExtracted = true;
                        return;
                    }
                }
            }
            
            // Strategy 4: Joker FROM HAND + 1 hand card + 1 extracted board card
            var jokerFromHandPlans = chainExtractor.FindJokerFromHandWithExtractionPlans();
            Debug.Log($"<color=magenta>Found {jokerFromHandPlans.Count} joker-from-hand extraction plans</color>");
            
            foreach (var plan in jokerFromHandPlans)
            {
                if (IsPlanStillValid(plan) && chainExtractor.ValidatePlan(plan))
                {
                    Debug.Log($"<color=magenta>JOKER FROM HAND: {plan}</color>");
                    if (ExecuteJokerFromHandPlan(plan))
                    {
                        this.chainExtracted = true;
                        return;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"<color=red>MaximizeChainExtractions ERROR: {ex.Message}\n{ex.StackTrace}</color>");
        }
    }
    
    /// <summary>
    /// Execute a joker extraction plan (2 hand cards + 1 joker from board)
    /// </summary>
    private bool ExecuteJokerPlan(ChainExtractor.ExtractionPlan plan)
    {
        try
        {
            if (plan.HandCards == null || plan.HandCards.Count < 2) return false;
            if (plan.BoardCards == null || plan.BoardCards.Count < 1) return false;
            
            // Extract joker from board
            var jokerExtract = plan.BoardCards[0];
            ExtractSingleCardFromBoard(jokerExtract.Card, jokerExtract.SetPosition, jokerExtract.IndexInSet);
            
            // Build new set
            CardsSet newSet = new CardsSet();
            List<Card> allCards = new List<Card>(plan.HandCards);
            allCards.Add(jokerExtract.Card);
            
            // Sort for runs
            if (plan.IsRun)
            {
                // Put joker in right position
                allCards = allCards.OrderBy(c => c.Number == Constants.JokerRank ? int.MaxValue : c.Number).ToList();
                
                // Determine joker position
                var nonJokers = allCards.Where(c => c.Number != Constants.JokerRank).OrderBy(c => c.Number).ToList();
                if (nonJokers.Count >= 2)
                {
                    int first = nonJokers[0].Number;
                    int second = nonJokers[1].Number;
                    
                    // Joker goes before, between, or after
                    allCards.Clear();
                    if (first > 1)
                    {
                        // Joker at start
                        allCards.Add(jokerExtract.Card);
                        allCards.AddRange(nonJokers);
                    }
                    else
                    {
                        // Joker at end
                        allCards.AddRange(nonJokers);
                        allCards.Add(jokerExtract.Card);
                    }
                }
            }
            
            foreach (Card card in allCards)
            {
                newSet.AddCardToEnd(card);
            }
            
            // Play the set
            gameBoard.PlayCardSetOnBoard(newSet);
            
            Debug.Log($"<color=green>Executed joker plan successfully!</color>");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ExecuteJokerPlan error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Verify that an extraction plan is still valid given current board state
    /// </summary>
    private bool IsPlanStillValid(ChainExtractor.ExtractionPlan plan)
    {
        try
        {
            if (plan == null) return false;
            
            // For joker plans, check HandCards instead of HandCard
            if (plan.HandCards != null && plan.HandCards.Count > 0)
            {
                foreach (var hc in plan.HandCards)
                {
                    if (hc == null || !myPlayer.GetPlayerHand().Contains(hc)) return false;
                }
            }
            else if (plan.HandCard != null)
            {
                if (!myPlayer.GetPlayerHand().Contains(plan.HandCard)) return false;
            }
            else
            {
                return false;
            }
            
            // Check all board cards are still extractable
            if (plan.BoardCards == null) return false;
            
            foreach (var extractable in plan.BoardCards)
            {
                if (extractable == null || extractable.Card == null || extractable.SetPosition == null) 
                    return false;
                
                CardsSet set = gameBoard.board.GetCardsSet(extractable.SetPosition);
                if (set == null) return false;
                if (!set.IsContainsCard(extractable.Card)) return false;
                
                // Verify still extractable (set has enough cards)
                if (set.isGroupOfColors && set.GetDeckLength() < Constants.MaxInGroup) return false;
                if (set.isRun && set.GetDeckLength() <= Constants.MinInRun) return false;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Execute a chain extraction plan
    /// </summary>
    private bool ExecuteChainPlan(ChainExtractor.ExtractionPlan plan)
    {
        try
        {
            Debug.Log($"<color=green>Executing plan: {plan}</color>");
            
            List<Card> extractedCards = new List<Card>();
            
            // Handle prerequisite moves first
            foreach (var extractable in plan.BoardCards)
            {
                if (extractable == null) continue;
                
                if (extractable.RequiresPreExtraction && extractable.PrerequisiteExtraction != null)
                {
                    var enabler = extractable.PrerequisiteExtraction;
                    
                    Debug.Log($"<color=yellow>Pre-extraction: {enabler.Card} from set {enabler.SetPosition?.GetId()}</color>");
                    
                    // Extract enabler from its source set
                    ExtractSingleCardFromBoard(enabler.Card, enabler.SetPosition, enabler.IndexInSet);
                    
                    // Add enabler to the target set
                    AddCardToExistingSet(enabler.Card, extractable.SetPosition);
                }
            }
            
            // Now extract all the board cards
            foreach (var extractable in plan.BoardCards)
            {
                if (extractable == null || extractable.Card == null) continue;
                
                Card card = extractable.Card;
                Debug.Log($"<color=yellow>Extracting: {card} from set {extractable.SetPosition?.GetId()}</color>");
                
                ExtractSingleCardFromBoard(card, extractable.SetPosition, extractable.IndexInSet);
                extractedCards.Add(card);
            }
            
            // Build the new set
            CardsSet newSet = new CardsSet();
            List<Card> allCards = new List<Card>();
            
            if (plan.HandCard != null)
            {
                allCards.Add(plan.HandCard);
            }
            allCards.AddRange(extractedCards);
            
            // Sort for runs
            if (plan.IsRun)
            {
                allCards = allCards.OrderBy(c => c.Number == Constants.JokerRank ? 999 : c.Number).ToList();
            }
            
            foreach (Card card in allCards)
            {
                if (card != null)
                    newSet.AddCardToEnd(card);
            }
            
            // Play the new set on board
            Debug.Log($"<color=green>Playing new set with {newSet.GetDeckLength()} cards</color>");
            gameBoard.PlayCardSetOnBoard(newSet);
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"<color=red>ExecuteChainPlan ERROR: {ex.Message}\n{ex.StackTrace}</color>");
            return false;
        }
    }
    
    /// <summary>
    /// Execute a joker-from-hand extraction plan (joker from hand + 1 hand card + 1 extracted board card)
    /// </summary>
    private bool ExecuteJokerFromHandPlan(ChainExtractor.ExtractionPlan plan)
    {
        try
        {
            Debug.Log($"<color=magenta>Executing joker-from-hand plan: {plan}</color>");
            
            // Get the joker and regular card from hand
            if (plan.HandCards == null || plan.HandCards.Count < 2)
            {
                Debug.LogError("ExecuteJokerFromHandPlan: Not enough hand cards");
                return false;
            }
            
            Card jokerCard = plan.HandCards.FirstOrDefault(c => c != null && c.Number == Constants.JokerRank);
            Card regularCard = plan.HandCards.FirstOrDefault(c => c != null && c.Number != Constants.JokerRank);
            
            if (jokerCard == null || regularCard == null)
            {
                Debug.LogError("ExecuteJokerFromHandPlan: Missing joker or regular card");
                return false;
            }
            
            // Extract card from board
            if (plan.BoardCards == null || plan.BoardCards.Count < 1)
            {
                Debug.LogError("ExecuteJokerFromHandPlan: No board cards to extract");
                return false;
            }
            
            var extractable = plan.BoardCards[0];
            if (extractable?.Card == null)
            {
                Debug.LogError("ExecuteJokerFromHandPlan: Invalid extractable card");
                return false;
            }
            
            Debug.Log($"<color=yellow>Extracting: {extractable.Card} from set {extractable.SetPosition?.GetId()}</color>");
            ExtractSingleCardFromBoard(extractable.Card, extractable.SetPosition, extractable.IndexInSet);
            Card extractedCard = extractable.Card;
            
            // Build the new set
            CardsSet newSet = new CardsSet();
            List<Card> allCards = new List<Card> { jokerCard, regularCard, extractedCard };
            
            if (plan.IsRun)
            {
                // Sort non-jokers by number, place joker appropriately
                var nonJokers = allCards.Where(c => c.Number != Constants.JokerRank).OrderBy(c => c.Number).ToList();
                
                if (nonJokers.Count >= 2)
                {
                    int num1 = nonJokers[0].Number;
                    int num2 = nonJokers[1].Number;
                    
                    allCards.Clear();
                    
                    // Determine joker position based on gap
                    if (num2 - num1 == 2)
                    {
                        // Joker fills middle gap: num1, Joker, num2
                        allCards.Add(nonJokers[0]);
                        allCards.Add(jokerCard);
                        allCards.Add(nonJokers[1]);
                    }
                    else if (num1 > 1 && num2 == num1 + 1)
                    {
                        // Joker at start
                        allCards.Add(jokerCard);
                        allCards.AddRange(nonJokers);
                    }
                    else
                    {
                        // Joker at end
                        allCards.AddRange(nonJokers);
                        allCards.Add(jokerCard);
                    }
                }
            }
            // For groups, order doesn't matter
            
            foreach (Card card in allCards)
            {
                if (card != null)
                    newSet.AddCardToEnd(card);
            }
            
            // Play the new set on board
            Debug.Log($"<color=green>Playing joker-from-hand set with {newSet.GetDeckLength()} cards</color>");
            gameBoard.PlayCardSetOnBoard(newSet);
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"<color=red>ExecuteJokerFromHandPlan ERROR: {ex.Message}\n{ex.StackTrace}</color>");
            return false;
        }
    }
    
    /// <summary>
    /// Extract a single card from the board
    /// </summary>
    private void ExtractSingleCardFromBoard(Card card, SetPosition setPosition, int cardIndex)
    {
        CardsSet sourceSet = gameBoard.board.GetCardsSet(setPosition);
        if (sourceSet == null) return;
        
        card.OldPosition = card.Position;
        int index = sourceSet.RemoveCard(card);
        
        if (sourceSet.set.Count == 0)
        {
            int key = gameBoard.GetKeyFromPosition(card.OldPosition);
            gameBoard.board.RemoveSetFromBothDic(key);
        }
        else if (index == 0 || index == sourceSet.set.Count)
        {
            gameBoard.board.HandleBeginningAndEndKeysUpdate(card, sourceSet, setPosition, index);
        }
        else
        {
            gameBoard.board.HandleMiddleSplit(card, sourceSet, setPosition, index);
        }
        
        // Visual: Move card off its current slot (will be repositioned when new set is played)
        if (card.transform.parent != null)
        {
            card.transform.SetParent(null);
        }
        
        gameBoard.AddCardToMovesStack(card);
    }
    
    /// <summary>
    /// Add a card to an existing set on the board
    /// </summary>
    private void AddCardToExistingSet(Card card, SetPosition targetSetPosition)
    {
        CardsSet targetSet = gameBoard.board.GetCardsSet(targetSetPosition);
        if (targetSet == null) return;
        
        // Determine where to add (beginning or end)
        if (targetSet.CanAddCardLast(card))
        {
            int tileSlot = targetSet.GetLastCard().Position.GetTileSlot() + 1;
            card.Position = new CardPosition(tileSlot);
            uiManager.MoveCardToBoard(card, tileSlot, false);
            gameBoard.MoveCardFromPlayerHandToGameBoard(card, RemoveOption.DontRemove);
        }
        else if (targetSet.CanAddCardFirst(card))
        {
            int tileSlot = targetSet.GetFirstCard().Position.GetTileSlot() - 1;
            card.Position = new CardPosition(tileSlot);
            uiManager.MoveCardToBoard(card, tileSlot, false);
            gameBoard.MoveCardFromPlayerHandToGameBoard(card, RemoveOption.DontRemove);
        }
        else
        {
            // Need to rearrange the set
            gameBoard.RearrangeCardsSet(targetSetPosition, card, AddPosition.End);
        }
        
        gameBoard.AddCardToMovesStack(card);
    }


    // This method is used to maximize the partial drops
    // it extracts the maximum valid run sets and group sets with the length of 2, (after dropping all valids one)
    // and then it tries to extract cards from the board without breaking the validity of the sets
    // and then drop the set with the extracted card + update the visuals and data structures
    // if it can't extract cards from the board, it will drop the set with a joker if exist in hand
    // O(n*logm) where n is the number of cards in the player hand and m is the number of partial sets

    public void MaximizePartialDrops()
    {
        this.partial = false;
        List<CardsSet> dropWithJoker = new List<CardsSet>();
        List<CardsSet> setsRun = ExtractMaxValidRunSets(myPlayer.GetPlayerHand().SortedByRun(), Constants.MaxPartialSet, Constants.MaxPartialSet);
        foreach (CardsSet set in setsRun)
        {
            set.isRun = true;
            CardInfo info = FindExtractableCardsFromBoard(set);
            if (info != null)
                ExtractCardAndReArrange(info, set);
            else
                dropWithJoker.Add(set);
        }
        TryToDropSetsWithJoker(dropWithJoker);
        dropWithJoker = new List<CardsSet>();
        List<CardsSet> setsGroup = ExtractMaxValidGroupSets(myPlayer.GetPlayerHand().SortedByGroup(), Constants.MaxPartialSet, Constants.MaxPartialSet);
        foreach (CardsSet set in setsGroup)
        {
            set.isGroupOfColors = true;
            CardInfo info = FindExtractableCardsFromBoard(set);
            if (info != null)
                ExtractCardAndReArrange(info, set);
            else
                dropWithJoker.Add(set);
        }
        TryToDropSetsWithJoker(dropWithJoker);
    }

    private void TryToDropSetsWithJoker(List<CardsSet> dropWithJoker)
    {
        Card jokerCard = myPlayer.GetPlayerHand().GetJoker();

        if (jokerCard != null && dropWithJoker.Count > Constants.EmptyCardsSet)
        {
            // Remove the joker card from the player's hand

            // Method to add joker to set
            Action<CardsSet, Card> addJokerToSet = (set, card) =>
            {
                if (set.CanAddCardFirst(card))
                {
                    set.AddCardToBeginning(card);
                }
                else if (set.CanAddCardLast(card))
                {
                    set.AddCardToEnd(card);
                }
            };

            // Try to add the joker to the first set
            addJokerToSet(dropWithJoker[0], jokerCard);

            // Play the first set on the board
            gameBoard.PlayCardSetOnBoard(dropWithJoker[0]);

            // Check if there's a second set and try to add another joker
            if (dropWithJoker.Count > 1)
            {
                jokerCard = myPlayer.GetPlayerHand().GetJoker(); // Get another joker card from the player's hand
                if (jokerCard != null)
                {
                    // Try to add the joker to the second set
                    addJokerToSet(dropWithJoker[1], jokerCard);

                    // Play the second set on the board
                    gameBoard.PlayCardSetOnBoard(dropWithJoker[1]);
                }
            }
        }
    }

    private void ExtractCardAndReArrange(CardInfo info, CardsSet set)
    {
        CardsSet setInBoard = gameBoard.board.GetCardsSet(info.GetSetPosition());
        this.partial = true;
        gameBoard.PlayCardSetOnBoard(set, 1, info.GetPosition()); // Drop two cards from the player's hand
        if (info.GetCardIndex() == 0 || info.GetCardIndex() == setInBoard.GetDeckLength() - 1)
        {

            if (info.GetPosition() == AddPosition.Beginning)
                HandleBeginningPosition(info, set, setInBoard);
            else
                HandleEndPosition(info, set, setInBoard);
        }
        else // extract card from a middle position => split the set or squeeze it
        {
            if (setInBoard.isGroupOfColors)
                HandleRearrangeGroup(info, set, setInBoard); // error
            else
                HandleRearrangeRun(info, set);
        }
    }

    // handle the re-arrangement of the group of colors - squeeze the set
    private void HandleRearrangeGroup(CardInfo info, CardsSet set, CardsSet setInBoard)
    {
        if (info.GetPosition() == AddPosition.Beginning)
            HandleBeginningPosition(info, set, setInBoard);
        else
            HandleEndPosition(info, set, setInBoard);
    }
    // handle the re-arrangement of the run - split the set
    private void HandleRearrangeRun(CardInfo info, CardsSet set)
    {
        CardsSet setInBoard = gameBoard.board.GetCardsSet(info.GetSetPosition());
        print(setInBoard.ToString());
        SplitSet(info.GetSetPosition(), info.GetCardIndex(), RemoveOption.Remove);

        if (info.GetPosition() == AddPosition.Beginning)
        {
            uiManager.MoveCardToBoard(info.GetCard(), set.GetFirstCard().Position.GetTileSlot() - 1, false);
            // Remove last key and add new key
            gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(set.GetFirstCard().Position));
            // Update the card position
            info.GetCard().Position.SetTileSlot(set.GetFirstCard().Position.GetTileSlot() - 1);
            SetPosition setPosition = new SetPosition(gameBoard.board.GetSetCount() - 2);
            gameBoard.board.SetSetPosition(gameBoard.GetKeyFromPosition(info.GetCard().Position), setPosition);
            gameBoard.board.GetGameBoardValidSetsTable()[setPosition].AddCardToBeginning(info.GetCard());
        }
        else
        {
            uiManager.MoveCardToBoard(info.GetCard(), set.GetLastCard().Position.GetTileSlot() + 1, false);
            // Remove last key and add new key
            gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(set.GetLastCard().Position));
            // Update the card position
            info.GetCard().Position.SetTileSlot(set.GetLastCard().Position.GetTileSlot() + 1);
            SetPosition setPosition = new SetPosition(gameBoard.board.GetSetCount() - 2);
            gameBoard.board.SetSetPosition(gameBoard.GetKeyFromPosition(info.GetCard().Position), setPosition);
            gameBoard.board.GetGameBoardValidSetsTable()[setPosition].AddCardToEnd(info.GetCard());
        }
    }

    private void HandleBeginningPosition(CardInfo info, CardsSet set, CardsSet setInBoard)
    {
        uiManager.MoveCardToBoard(info.GetCard(), set.GetFirstCard().Position.GetTileSlot() - 1, false);
        // Remove last key and add new key
        gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(set.GetFirstCard().Position));
        UpdateSetPosition(info, setInBoard);
        // Update the card position
        info.GetCard().Position.SetTileSlot(set.GetFirstCard().Position.GetTileSlot() - 1);
        SetPosition setPosition = new SetPosition(gameBoard.board.GetSetCount() - 1);
        gameBoard.board.SetSetPosition(gameBoard.GetKeyFromPosition(set.GetFirstCard().Position) - 1, setPosition);
        gameBoard.board.GetGameBoardValidSetsTable()[setPosition].AddCardToBeginning(info.GetCard());
    }

    private void HandleEndPosition(CardInfo info, CardsSet set, CardsSet setInBoard)
    {
        uiManager.MoveCardToBoard(info.GetCard(), set.GetLastCard().Position.GetTileSlot() + 1, false);
        // Remove last key and add new key
        gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(set.GetLastCard().Position));

        UpdateSetPosition(info, setInBoard);
        // Update the card position
        info.GetCard().Position.SetTileSlot(set.GetLastCard().Position.GetTileSlot() + 1);
        SetPosition setPosition = new SetPosition(gameBoard.board.GetSetCount() - 1);
        gameBoard.board.SetSetPosition(gameBoard.GetKeyFromPosition(set.GetLastCard().Position) + 1, setPosition);
        gameBoard.board.GetGameBoardValidSetsTable()[setPosition].AddCardToEnd(info.GetCard());
    }

    private void UpdateSetPosition(CardInfo info, CardsSet setInBoard)
    {
        if (info.GetCardIndex() == 0)
        {
            gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(setInBoard.GetFirstCard().Position));
            setInBoard.RemoveCard(info.GetCard());
            gameBoard.board.UpdateKeySingleCardsSet(gameBoard.GetKeyFromPosition(setInBoard.GetFirstCard().Position), info.GetSetPosition());
        }
        else if (info.GetCardIndex() == setInBoard.GetDeckLength() - 1)
        {
            gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(setInBoard.GetLastCard().Position));
            setInBoard.RemoveCard(info.GetCard());
            gameBoard.board.UpdateKeySingleCardsSet(gameBoard.GetKeyFromPosition(setInBoard.GetLastCard().Position), info.GetSetPosition());
        }
        else if (info.GetCardIndex() == 1)
        {
            //squeeze the set to the right
            gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(setInBoard.GetFirstCard().Position)); // remove the most left pointer
            uiManager.MoveCardToBoard(setInBoard.GetFirstCard(), setInBoard.GetFirstCard().Position.GetTileSlot() + 1, false);
            setInBoard.RemoveCard(info.GetCard());
            setInBoard.GetFirstCard().Position.Column = setInBoard.GetFirstCard().Position.Column + 1; // get a new position to point to that set
            gameBoard.board.UpdateKeySingleCardsSet(gameBoard.GetKeyFromPosition(setInBoard.GetFirstCard().Position), info.GetSetPosition());
        }
        else if (info.GetCardIndex() == 2)
        {
            //squeeze the set to the left
            gameBoard.board.RemoveSetPosition(gameBoard.GetKeyFromPosition(setInBoard.GetLastCard().Position)); // remove the most right pointer
            uiManager.MoveCardToBoard(setInBoard.GetLastCard(), setInBoard.GetLastCard().Position.GetTileSlot() - 1, false);
            setInBoard.RemoveCard(info.GetCard());
            setInBoard.GetLastCard().Position.Column = setInBoard.GetLastCard().Position.Column - 1;
            // get a new position to point to that set
            gameBoard.board.UpdateKeySingleCardsSet(gameBoard.GetKeyFromPosition(setInBoard.GetLastCard().Position), info.GetSetPosition());
        }
    }

    // O(n) where n is the number of cards in the board
    private CardInfo FindExtractableCardsFromBoard(CardsSet partialSet)
    {
        //filter the foreach set only for those whos their length is bigger than MinInRun
        foreach (SetPosition sp in gameBoard.board.GetGameBoardValidSetsTable().Keys) // Iterate over the sets on the game board
        {
            CardsSet setInBoard = gameBoard.board.GetGameBoardValidSetsTable()[sp]; // get the current set from the game board


            if (setInBoard.GetDeckLength() == Constants.MaxInGroup && setInBoard.isGroupOfColors) // if possible 1 of 4 cards in a group
            {
                CardInfo info = FindCardInGroup(setInBoard, partialSet, sp);
                if (info != null) // find the card in the group (first, last, middle)
                    return info; // return the card info
            }
            else if (setInBoard.isRun && setInBoard.GetDeckLength() > Constants.MinInRun) // if the set is a run
            {
                CardInfo info = FindCardInRun(setInBoard, partialSet, sp);
                if (info != null)  // find the card in the run (first, last, middle)
                    return info; // return the card info
            }
        }
        return null; // if no card was found
    }

    // O(4) = O(1), where 4 is the number of cards in a group, O(1) because the number of cards in a group is constant
    private CardInfo FindCardInGroup(CardsSet setInBoard, CardsSet partialSet, SetPosition sp)
    {
        int index = 0; // index of the card in the set
        foreach (Card card in setInBoard.set)
        {
            if (partialSet.CanAddCardFirst(card) || partialSet.CanAddCardLast(card))
                return new CardInfo(card, sp, partialSet.CanAddCardLast(card) ? AddPosition.End : AddPosition.Beginning, index); // return the card info
            index++; // increment the index per iteration
        }
        return null; // if no card was found 
    }


    // returns the card info of the card in the run (first, last, middle)
    private CardInfo FindCardInRun(CardsSet setInBoard, CardsSet partialSet, SetPosition sp)
    {
        Card firstCard = setInBoard.GetFirstCard(); // get the first card in the set
        Card lastCard = setInBoard.GetLastCard(); // get the last card in the set
        if (partialSet.CanAddCardLast(firstCard) || partialSet.CanAddCardFirst(firstCard)) // if the card can be added to the first card in the run
            return new CardInfo(firstCard, sp, partialSet.CanAddCardLast(firstCard) ? AddPosition.End : AddPosition.Beginning, 0); // Return the first card if it can be added to the beginning or end of the partial set.

        else if (partialSet.CanAddCardLast(lastCard) || partialSet.CanAddCardFirst(lastCard)) // if the card can be added to the last card in the run
            return new CardInfo(lastCard, sp, partialSet.CanAddCardLast(lastCard) ? AddPosition.End : AddPosition.Beginning, setInBoard.GetDeckLength() - 1); // Return the last card if it can be added to the beginning or end of the partial set.

        else if (setInBoard.GetDeckLength() > Constants.MinSetLengthForMiddleBreak) // if the run is long enough to break
            foreach (Card card in setInBoard.GetMiddleCards())
                if (partialSet.CanAddCardFirst(card) || partialSet.CanAddCardLast(card))
                    return new CardInfo(card, sp, partialSet.CanAddCardLast(card) ? AddPosition.End : AddPosition.Beginning, card.Number - firstCard.Number); // Return the middle card if it can be added to the beginning or end of the partial set.
        return null;
    }


    // Assigns free cards to existing sets on the game board, rearrange visualy if needed.
    // O(n*logm) where n is the number of cards in the player hand and m is the number of sets on the game board
    public void AssignFreeCardsToExistsSets()
    {
        this.added = false; // reset the added flag, for the confirmation
        List<Card> cardsToRemove = new List<Card>(); // list of cards to remove from the player hand, the cards we assigned to the game board
        foreach (Card card in myPlayer.GetPlayerHand()) // iterate over the player hand
        {
            bool found = false; // flag to indicate if we found a set to add the card to
            int offset = -1; // offset for the middle run
            List<SetPosition> keys = gameBoard.board.GetGameBoardValidSetsTable().Keys.ToList(); // get the keys of the sets on the game board
            keys.ToArray(); // convert the keys to an array
            for (int i = 0; i < keys.Count && !found; i++) // iterate over the keys such way that we break the loop if we found a set to add the card to
            {
                SetPosition key = keys[i]; // get the key for the current iteration
                CardsSet set = gameBoard.board.GetGameBoardValidSetsTable()[key]; // get the current set from the game board
                if (set.CanAddCardLast(card)) // if the card can be added to the end of the set
                {
                    found = true; // indicate that we found a set to add the card to, break the loop
                    this.added = true; // indicate that we added a card for the confirmation
                    cardsToRemove.Add(card); // add the card to the list of cards to remove
                    if (gameBoard.IsSpaceForCard(true, set)) // if there is space for the card in the game board play with O(1)
                        MoveCardToNextSlot(card, set.GetLastCard().Position.GetTileSlot() + 1); // move the card to the next slot, given true - play without removing the card from the player hand (we handle that at the end of the method), O(1)
                    else
                        RearrangeCardsSet(key, card, AddPosition.End); // rearrange the cards in the set, 
                }
                else if (set.CanAddCardFirst(card)) // if the card can be added to the beginning of the set
                {
                    found = true; // indicate that we found a set to add the card to, break the loop
                    this.added = true; // indicate that we added a card for the confirmation
                    cardsToRemove.Add(card); // add the card to the list of cards to remove
                    if (gameBoard.IsSpaceForCard(false, set)) // if there is space for the card in the game board play with O(1)
                        MoveCardToPreviousSlot(card, set.GetFirstCard().Position.GetTileSlot() - 1); // move the card to the previous slot, given false - play without removing the card from the player hand (we handle that at the end of the method), O(1)
                    else
                        RearrangeCardsSet(key, card, AddPosition.Beginning);
                }
                else if ((offset = set.CanAddCardMiddleRun(card)) != -1) // if the card can be added to the middle of the run
                {
                    found = true; // indicate that we found a set to add the card to, break the loop
                    this.added = true; // indicate that we added a card for the confirmation
                    cardsToRemove.Add(card); // add the card to the list of cards to remove
                    SplitAndRearrangeCardsSet(key, card, offset); // split the set and rearrange the cards in the game board, O(n) where n is the number of cards in the set
                }
            }
        }
        RemoveCardsFromPlayerHand(cardsToRemove); // remove the cards from the player hand after assigning them to the game board
    }

    // Moves the card to the next slot in the game board, O(1)
    private void MoveCardToNextSlot(Card card, int nextSlot)
    {
        // Save the old position of the card
        card.OldPosition = card.Position;
        // Set the new position of the card
        card.Position.SetTileSlot(nextSlot);
        // Play the card on the board, given false - play without removing the card from the player hand (we handle that at the end of the method)
        gameBoard.PlayCardOnBoard(card, nextSlot, RemoveOption.DontRemove);
    }

    // Moves the card to the previous slot in the game board, O(1)
    private void MoveCardToPreviousSlot(Card card, int previousSlot)
    {
        // Save the old position of the card
        card.OldPosition = card.Position;
        // Set the new position of the card
        card.Position.SetTileSlot(previousSlot);
        // Play the card on the board, given false - play without removing the card from the player hand (we handle that at the end of the method)
        gameBoard.PlayCardOnBoard(card, previousSlot, RemoveOption.DontRemove);
    }

    // Rearranges the cards in the game board, O(n) where n is the number of cards in the set
    private void RearrangeCardsSet(SetPosition key, Card card, AddPosition addPosition)
    {
        gameBoard.RearrangeCardsSet(key, card, addPosition); // rearrange the cards in the set, given isAddingToLast - adding the card at the end of the set
    }

    // Splits the set and rearranges the cards in the game board, O(n) where n is the number of cards in the set

    private SetPosition SplitSet(SetPosition key, int offset, RemoveOption removeOption = RemoveOption.DontRemove)
    {
        // Get the set from the game board 
        CardsSet Oldset = gameBoard.board.GetGameBoardValidSetsTable()[key];
        
        // Safety check: don't try to uncombine more cards than exist in the set
        int setLength = Oldset.GetDeckLength();
        if (offset > setLength)
        {
            Debug.LogWarning($"SplitSet: offset ({offset}) > set length ({setLength}), clamping");
            offset = setLength;
        }
        
        // Uncombine the set into two sets, the first one with the offset (appending at the end the remaining card), the second one with the rest of the cards
        CardsSet newSet = Oldset.UnCombine(offset);
        
        // Create a new set position with new id
        if (removeOption == RemoveOption.Remove && Oldset.GetDeckLength() > 0)
        {
            Oldset.set.RemoveFirst();
        }
        
        SetPosition newSetPos = new SetPosition(gameBoard.board.GetSetCountAndInc());
        // Add the new set to the game board
        gameBoard.board.AddCardsSet(newSetPos, newSet);
        
        // Update the keys of the cards in the new set
        if (newSet.GetDeckLength() > 0)
        {
            gameBoard.board.UpdateKeyMultiCardsSet(gameBoard.GetKeyFromPosition(newSet.GetFirstCard().Position), gameBoard.GetKeyFromPosition(newSet.GetLastCard().Position), newSetPos);
        }
        
        // Update the keys of the cards in the old set (only if it still has cards)
        if (Oldset.GetDeckLength() > 0)
        {
            gameBoard.board.UpdateKeyMultiCardsSet(gameBoard.GetKeyFromPosition(Oldset.GetFirstCard().Position), gameBoard.GetKeyFromPosition(Oldset.GetLastCard().Position), key);
        }
        else
        {
            // Old set is empty, remove it from the board
            gameBoard.board.GetGameBoardValidSetsTable().Remove(key);
        }
        
        return newSetPos;
    }
    private void SplitAndRearrangeCardsSet(SetPosition key, Card card, int offset)
    {
        SetPosition newSetPos = SplitSet(key, offset);
        // Rearrange the set with the new card     
        gameBoard.RearrangeCardsSet(newSetPos, card, AddPosition.End);
    }

    // Removes the cards from the player hand after assigning them to the game board
    // in a separate list (and method) to avoid concurrent modification exception
    private void RemoveCardsFromPlayerHand(List<Card> cardsToRemove)
    {
        foreach (Card card in cardsToRemove) // iterate over the cards to remove
        {
            myPlayer.RemoveCardFromList(card); // remove the card from the player hand
        }
    }
}

