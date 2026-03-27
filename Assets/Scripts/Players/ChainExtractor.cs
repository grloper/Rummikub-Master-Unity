using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles advanced AI strategies for chain extractions and multi-card board manipulations.
/// Features:
/// 1. Extract multiple cards from board to complete a partial in hand
/// 2. Chain extractions (extract A to enable extracting B)
/// 3. Extract jokers from board sets to use for partials
/// 4. Comprehensive debug logging
/// </summary>
public class ChainExtractor
{
    private readonly GameBoard gameBoard;
    private readonly Player player;
    private readonly UImanager uiManager;
    private const bool DEBUG_MODE = true;

    public ChainExtractor(GameBoard gameBoard, Player player, UImanager uiManager)
    {
        this.gameBoard = gameBoard;
        this.player = player;
        this.uiManager = uiManager;
    }

    #region Data Classes
    
    public class ExtractionPlan
    {
        public Card HandCard { get; set; }
        public List<Card> HandCards { get; set; }
        public List<ExtractableCard> BoardCards { get; set; }
        public bool IsRun { get; set; }
        public string Description { get; set; }

        public ExtractionPlan()
        {
            BoardCards = new List<ExtractableCard>();
            HandCards = new List<Card>();
        }
        
        public override string ToString()
        {
            string handStr = HandCard != null ? HandCard.ToString() : "null";
            string boardStr = BoardCards.Count > 0 ? string.Join(", ", BoardCards.Select(b => b.Card?.ToString() ?? "null")) : "none";
            return $"[Plan: Hand={handStr}, Board=[{boardStr}], IsRun={IsRun}, Desc={Description}]";
        }
    }

    public class ExtractableCard
    {
        public Card Card { get; set; }
        public SetPosition SetPosition { get; set; }
        public int IndexInSet { get; set; }
        public bool RequiresPreExtraction { get; set; }
        public ExtractableCard PrerequisiteExtraction { get; set; }
        public bool IsJoker { get; set; }
        
        public override string ToString()
        {
            string cardStr = Card != null ? $"{Card.Color}{Card.Number}" : "null";
            return $"[{cardStr}@Set{SetPosition?.GetId()},idx={IndexInSet},joker={IsJoker}]";
        }
    }
    
    #endregion

    #region Debug Methods
    
    public void PrintDebugState(string context)
    {
        if (!DEBUG_MODE) return;
        
        Debug.Log($"<color=yellow>═══ CHAIN DEBUG: {context} ═══</color>");
        
        try
        {
            var boardSets = gameBoard.board.GetGameBoardValidSetsTable();
            Debug.Log($"<color=yellow>Board Sets ({boardSets.Count}):</color>");
            
            foreach (var kvp in boardSets)
            {
                if (kvp.Value == null) continue;
                CardsSet set = kvp.Value;
                string type = set.isRun ? "RUN" : (set.isGroupOfColors ? "GRP" : "???");
                string cards = "";
                foreach (Card c in set.set)
                {
                    if (c != null) cards += $"{c.Color.ToString()[0]}{c.Number}, ";
                }
                Debug.Log($"  [{kvp.Key.GetId()}] {type}({set.GetDeckLength()}): {cards}");
            }
            
            var extractables = GetAllExtractableCards();
            Debug.Log($"<color=cyan>Extractable ({extractables.Count}):</color>");
            foreach (var e in extractables.Take(10))
            {
                Debug.Log($"  {e}");
            }
            if (extractables.Count > 10) Debug.Log($"  ... and {extractables.Count - 10} more");
            
            Debug.Log($"<color=green>Hand:</color>");
            string hand = "";
            foreach (Card c in player.GetPlayerHand())
            {
                if (c != null) hand += $"{c.Color.ToString()[0]}{c.Number}, ";
            }
            Debug.Log($"  {hand}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Debug print error: {ex.Message}");
        }
        
        Debug.Log($"<color=yellow>═══ END DEBUG ═══</color>");
    }
    
    /// <summary>
    /// Validate that a plan makes sense (cards form valid set, no nulls, etc.)
    /// </summary>
    public bool ValidatePlan(ExtractionPlan plan)
    {
        try
        {
            if (plan == null) return false;
            
            // Collect all cards in the plan
            List<Card> allCards = new List<Card>();
            
            if (plan.HandCards != null && plan.HandCards.Count > 0)
            {
                allCards.AddRange(plan.HandCards.Where(c => c != null));
            }
            else if (plan.HandCard != null)
            {
                allCards.Add(plan.HandCard);
            }
            
            if (plan.BoardCards != null)
            {
                foreach (var bc in plan.BoardCards)
                {
                    if (bc?.Card != null) allCards.Add(bc.Card);
                }
            }
            
            // Must have at least 3 cards
            if (allCards.Count < 3) 
            {
                if (DEBUG_MODE) Debug.Log($"Plan invalid: only {allCards.Count} cards");
                return false;
            }
            
            // Check if they form a valid set
            CardsSet testSet = new CardsSet();
            foreach (var card in allCards)
            {
                testSet.AddCardToEnd(card);
            }
            
            bool valid = testSet.IsRun() || testSet.IsGroupOfColors();
            if (!valid && DEBUG_MODE)
            {
                Debug.Log($"Plan invalid: cards don't form valid set");
            }
            return valid;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ValidatePlan error: {ex.Message}");
            return false;
        }
    }
    
    #endregion

    #region Get Extractable Cards
    
    public List<ExtractableCard> GetAllExtractableCards()
    {
        List<ExtractableCard> extractables = new List<ExtractableCard>();
        
        try
        {
            var boardSets = gameBoard.board.GetGameBoardValidSetsTable();
            if (boardSets == null) return extractables;

            foreach (var kvp in boardSets)
            {
                SetPosition sp = kvp.Key;
                CardsSet set = kvp.Value;
                
                if (set == null || set.set == null || set.GetDeckLength() == 0) continue;

                // 4-card groups: can extract any card
                if (set.isGroupOfColors && set.GetDeckLength() == Constants.MaxInGroup)
                {
                    int idx = 0;
                    foreach (Card card in set.set)
                    {
                        if (card != null)
                        {
                            extractables.Add(new ExtractableCard
                            {
                                Card = card,
                                SetPosition = sp,
                                IndexInSet = idx,
                                RequiresPreExtraction = false,
                                IsJoker = card.Number == Constants.JokerRank
                            });
                        }
                        idx++;
                    }
                }
                // Runs with 4+ cards: can extract first or last
                else if (set.isRun && set.GetDeckLength() > Constants.MinInRun)
                {
                    Card firstCard = set.GetFirstCard();
                    Card lastCard = set.GetLastCard();
                    
                    if (firstCard != null)
                    {
                        extractables.Add(new ExtractableCard
                        {
                            Card = firstCard,
                            SetPosition = sp,
                            IndexInSet = 0,
                            RequiresPreExtraction = false,
                            IsJoker = firstCard.Number == Constants.JokerRank
                        });
                    }
                    
                    if (lastCard != null && lastCard != firstCard)
                    {
                        extractables.Add(new ExtractableCard
                        {
                            Card = lastCard,
                            SetPosition = sp,
                            IndexInSet = set.GetDeckLength() - 1,
                            RequiresPreExtraction = false,
                            IsJoker = lastCard.Number == Constants.JokerRank
                        });
                    }

                    // Middle cards from 7+ card runs
                    if (set.GetDeckLength() >= Constants.MinSetLengthForMiddleBreak)
                    {
                        try
                        {
                            var middleCards = set.GetMiddleCards();
                            int baseIdx = 3;
                            foreach (Card card in middleCards)
                            {
                                if (card != null)
                                {
                                    extractables.Add(new ExtractableCard
                                    {
                                        Card = card,
                                        SetPosition = sp,
                                        IndexInSet = baseIdx,
                                        RequiresPreExtraction = false,
                                        IsJoker = card.Number == Constants.JokerRank
                                    });
                                }
                                baseIdx++;
                            }
                        }
                        catch { /* Skip middle cards on error */ }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"GetAllExtractableCards error: {ex.Message}");
        }

        return extractables;
    }
    
    #endregion

    #region Joker Extraction
    
    /// <summary>
    /// Find extractable jokers that can complete 2-card partials in hand
    /// </summary>
    public List<ExtractionPlan> FindJokerExtractionPlans()
    {
        List<ExtractionPlan> plans = new List<ExtractionPlan>();
        
        try
        {
            var extractables = GetAllExtractableCards();
            var jokers = extractables.Where(e => e.IsJoker).ToList();
            
            if (jokers.Count == 0) return plans;
            
            if (DEBUG_MODE) Debug.Log($"<color=magenta>Found {jokers.Count} extractable joker(s)</color>");
            
            foreach (var jokerExtract in jokers)
            {
                // Check for 2-card partial runs in hand
                var handCards = player.GetPlayerHand().SortedByRun();
                for (int i = 0; i < handCards.Count - 1; i++)
                {
                    Card c1 = handCards[i];
                    Card c2 = handCards[i + 1];
                    
                    if (c1 == null || c2 == null) continue;
                    if (c1.Number == Constants.JokerRank || c2.Number == Constants.JokerRank) continue;
                    
                    // Consecutive same color = partial run
                    if (c1.Color == c2.Color && c2.Number == c1.Number + 1)
                    {
                        if (c1.Number > 1 || c2.Number < Constants.MaxRank)
                        {
                            plans.Add(new ExtractionPlan
                            {
                                HandCard = c1,
                                HandCards = new List<Card> { c1, c2 },
                                BoardCards = new List<ExtractableCard> { jokerExtract },
                                IsRun = true,
                                Description = $"Joker + [{c1.Number},{c2.Number}] run"
                            });
                        }
                    }
                }
                
                // Check for 2-card partial groups in hand
                var handCardsGroup = player.GetPlayerHand().SortedByGroup();
                for (int i = 0; i < handCardsGroup.Count - 1; i++)
                {
                    Card c1 = handCardsGroup[i];
                    Card c2 = handCardsGroup[i + 1];
                    
                    if (c1 == null || c2 == null) continue;
                    if (c1.Number == Constants.JokerRank || c2.Number == Constants.JokerRank) continue;
                    
                    // Same number, different color = partial group
                    if (c1.Number == c2.Number && c1.Color != c2.Color)
                    {
                        plans.Add(new ExtractionPlan
                        {
                            HandCard = c1,
                            HandCards = new List<Card> { c1, c2 },
                            BoardCards = new List<ExtractableCard> { jokerExtract },
                            IsRun = false,
                            Description = $"Joker + [{c1.Number},{c2.Number}] group"
                        });
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"FindJokerExtractionPlans error: {ex.Message}");
        }
        
        return plans;
    }
    
    #endregion

    #region Double Extraction (1 hand + 2 board)
    
    /// <summary>
    /// Find plays where 1 card from hand + 2 cards from board form a valid set
    /// </summary>
    public List<ExtractionPlan> FindSingleCardWithDoubleExtraction()
    {
        List<ExtractionPlan> plans = new List<ExtractionPlan>();
        
        try
        {
            List<ExtractableCard> extractables = GetAllExtractableCards();
            
            if (extractables.Count < 2) return plans;

            foreach (Card handCard in player.GetPlayerHand())
            {
                if (handCard == null || handCard.Number == Constants.JokerRank) continue;

                // Case A: Run - same color, consecutive numbers
                var sameColorCards = extractables
                    .Where(e => e.Card != null && e.Card.Color == handCard.Color && e.Card.Number != handCard.Number)
                    .ToList();

                for (int i = 0; i < sameColorCards.Count; i++)
                {
                    for (int j = i + 1; j < sameColorCards.Count; j++)
                    {
                        var card1 = sameColorCards[i];
                        var card2 = sameColorCards[j];
                        
                        if (card1.Card == null || card2.Card == null) continue;
                        
                        // Avoid breaking same set (unless 5+ cards)
                        if (card1.SetPosition.Equals(card2.SetPosition))
                        {
                            CardsSet sourceSet = gameBoard.board.GetCardsSet(card1.SetPosition);
                            if (sourceSet == null || sourceSet.GetDeckLength() < 5) continue;
                        }

                        List<int> nums = new List<int> { handCard.Number, card1.Card.Number, card2.Card.Number };
                        nums.Sort();

                        if (nums[2] - nums[1] == 1 && nums[1] - nums[0] == 1)
                        {
                            plans.Add(new ExtractionPlan
                            {
                                HandCard = handCard,
                                BoardCards = new List<ExtractableCard> { card1, card2 },
                                IsRun = true,
                                Description = $"Run: {nums[0]},{nums[1]},{nums[2]}"
                            });
                        }
                    }
                }

                // Case B: Group - same number, different colors
                var sameNumberCards = extractables
                    .Where(e => e.Card != null && e.Card.Number == handCard.Number && e.Card.Color != handCard.Color)
                    .ToList();

                for (int i = 0; i < sameNumberCards.Count; i++)
                {
                    for (int j = i + 1; j < sameNumberCards.Count; j++)
                    {
                        var card1 = sameNumberCards[i];
                        var card2 = sameNumberCards[j];
                        
                        if (card1.Card == null || card2.Card == null) continue;
                        if (card1.Card.Color == card2.Card.Color) continue;

                        plans.Add(new ExtractionPlan
                        {
                            HandCard = handCard,
                            BoardCards = new List<ExtractableCard> { card1, card2 },
                            IsRun = false,
                            Description = $"Group: {handCard.Number}x3"
                        });
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"FindSingleCardWithDoubleExtraction error: {ex.Message}");
        }

        return plans;
    }
    
    #endregion

    #region Chain Extraction (add to 3-group to enable extraction)
    
    /// <summary>
    /// Find chain extractions: add card to 3-group to make it 4, then extract needed card
    /// </summary>
    public List<ExtractionPlan> FindChainExtractionPlans()
    {
        List<ExtractionPlan> plans = new List<ExtractionPlan>();
        
        try
        {
            var boardSets = gameBoard.board.GetGameBoardValidSetsTable();
            var extractables = GetAllExtractableCards();

            foreach (var kvp in boardSets)
            {
                CardsSet set = kvp.Value;
                if (set == null || !set.isGroupOfColors || set.GetDeckLength() != Constants.MinInGroup) 
                    continue;

                // Find missing color in this 3-card group
                HashSet<CardColor> existingColors = new HashSet<CardColor>();
                int groupNumber = -1;

                foreach (Card c in set.set)
                {
                    if (c != null && c.Number != Constants.JokerRank)
                    {
                        existingColors.Add(c.Color);
                        groupNumber = c.Number;
                    }
                }

                if (groupNumber <= 0) continue;

                CardColor? missingColor = null;
                foreach (CardColor color in System.Enum.GetValues(typeof(CardColor)))
                {
                    if (!existingColors.Contains(color))
                    {
                        missingColor = color;
                        break;
                    }
                }

                if (!missingColor.HasValue) continue;

                // Can we extract the missing card from another set?
                var enablerCard = extractables.FirstOrDefault(e =>
                    e.Card != null && e.Card.Number == groupNumber && e.Card.Color == missingColor.Value);

                if (enablerCard == null) continue;
                
                // After adding enabler, we can extract from this group
                int targetIdx = 0;
                foreach (Card groupCard in set.set)
                {
                    if (groupCard == null) { targetIdx++; continue; }
                    
                    foreach (Card handCard in player.GetPlayerHand())
                    {
                        if (handCard == null) continue;
                        
                        if (CanFormPartialWith(handCard, groupCard))
                        {
                            plans.Add(new ExtractionPlan
                            {
                                HandCard = handCard,
                                BoardCards = new List<ExtractableCard>
                                {
                                    new ExtractableCard
                                    {
                                        Card = groupCard,
                                        SetPosition = kvp.Key,
                                        IndexInSet = targetIdx,
                                        RequiresPreExtraction = true,
                                        PrerequisiteExtraction = enablerCard,
                                        IsJoker = groupCard.Number == Constants.JokerRank
                                    }
                                },
                                IsRun = handCard.Color == groupCard.Color,
                                Description = $"Chain: {enablerCard.Card}->{groupCard}"
                            });
                        }
                    }
                    targetIdx++;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"FindChainExtractionPlans error: {ex.Message}");
        }

        return plans;
    }
    
    private bool CanFormPartialWith(Card card1, Card card2)
    {
        if (card1 == null || card2 == null) return false;
        
        if (card1.Number == Constants.JokerRank || card2.Number == Constants.JokerRank)
            return true;
        
        if (card1.Color == card2.Color)
        {
            int diff = System.Math.Abs(card1.Number - card2.Number);
            return diff == 1 || diff == 2;
        }
        
        if (card1.Number == card2.Number && card1.Color != card2.Color)
            return true;
        
        return false;
    }
    
    #endregion

    #region Advanced Chain (multi-step)
    
    public List<ExtractionPlan> FindAdvancedChainPlans()
    {
        List<ExtractionPlan> plans = new List<ExtractionPlan>();
        
        try
        {
            var boardSets = gameBoard.board.GetGameBoardValidSetsTable();
            var extractables = GetAllExtractableCards();

            foreach (Card handCard in player.GetPlayerHand())
            {
                if (handCard == null || handCard.Number == Constants.JokerRank) continue;

                // Try to build runs starting at different positions
                for (int offset = -2; offset <= 0; offset++)
                {
                    int num1 = handCard.Number + offset;
                    int num2 = num1 + 1;
                    int num3 = num1 + 2;

                    if (num1 < 1 || num3 > Constants.MaxRank) continue;

                    List<int> neededNumbers = new List<int> { num1, num2, num3 };
                    neededNumbers.Remove(handCard.Number);

                    var plan = TryBuildChainForNumbers(handCard, neededNumbers, extractables, boardSets);
                    if (plan != null)
                    {
                        plans.Add(plan);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"FindAdvancedChainPlans error: {ex.Message}");
        }

        return plans;
    }

    private ExtractionPlan TryBuildChainForNumbers(Card handCard, List<int> neededNumbers,
        List<ExtractableCard> extractables, Dictionary<SetPosition, CardsSet> boardSets)
    {
        List<ExtractableCard> foundCards = new List<ExtractableCard>();

        foreach (int neededNum in neededNumbers)
        {
            // First try direct extraction
            var found = extractables.FirstOrDefault(e =>
                e.Card != null && e.Card.Number == neededNum && e.Card.Color == handCard.Color);

            if (found != null)
            {
                foundCards.Add(found);
                continue;
            }

            // Try chain approach via 3-card groups
            bool foundViaChain = false;
            foreach (var kvp in boardSets)
            {
                CardsSet set = kvp.Value;
                if (set == null || !set.isGroupOfColors || set.GetDeckLength() != Constants.MinInGroup)
                    continue;

                Card targetCard = null;
                int targetIdx = 0;
                foreach (Card c in set.set)
                {
                    if (c != null && c.Number == neededNum && c.Color == handCard.Color)
                    {
                        targetCard = c;
                        break;
                    }
                    targetIdx++;
                }

                if (targetCard == null) continue;

                // Find missing color for this group
                HashSet<CardColor> existingColors = new HashSet<CardColor>();
                foreach (Card c in set.set)
                {
                    if (c != null && c.Number != Constants.JokerRank)
                        existingColors.Add(c.Color);
                }

                CardColor? missingColor = null;
                foreach (CardColor color in System.Enum.GetValues(typeof(CardColor)))
                {
                    if (!existingColors.Contains(color))
                    {
                        missingColor = color;
                        break;
                    }
                }

                if (!missingColor.HasValue) continue;

                var enabler = extractables.FirstOrDefault(e =>
                    e.Card != null && e.Card.Number == neededNum && e.Card.Color == missingColor.Value);

                if (enabler != null)
                {
                    foundCards.Add(new ExtractableCard
                    {
                        Card = targetCard,
                        SetPosition = kvp.Key,
                        IndexInSet = targetIdx,
                        RequiresPreExtraction = true,
                        PrerequisiteExtraction = enabler,
                        IsJoker = targetCard.Number == Constants.JokerRank
                    });
                    foundViaChain = true;
                    break;
                }
            }

            if (!foundViaChain)
                return null; // Couldn't find this number
        }

        if (foundCards.Count == neededNumbers.Count)
        {
            return new ExtractionPlan
            {
                HandCard = handCard,
                BoardCards = foundCards,
                IsRun = true,
                Description = $"AdvChain: {handCard}+{string.Join(",", neededNumbers)}"
            };
        }

        return null;
    }
    
    #endregion

}
