using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Domain.Services
{
    public interface IPokerHandEvaluator
    {
        HandEvaluationResult Evaluate(Hand hand, IList<Player> players);
    }
    public interface IHandRankEvaluator
    {
        PokerHandRank EvaluateRank(IEnumerable<Card> holeCards, IEnumerable<Card> communityCards);
    }
    public class PokerHandEvaluator : IPokerHandEvaluator, IHandRankEvaluator
    {
        public HandEvaluationResult Evaluate(Hand hand, IList<Player> players)
        {
            // Csak a showdown-ra jogosultak
            var eligible = players
                .Where(p => p.PlayerStatus is not (PlayerStatus.Folded or PlayerStatus.Lost))
                .ToList();

            // Ha csak egy maradt, ő viszi mindent
            if (eligible.Count == 1)
            {
                var lone = eligible[0];
                var winner = new Winner
                {
                    HandId = hand.Id,
                    PlayerId = lone.Id,
                    Player = lone,
                    Pot = hand.Pot.MainPot + hand.Pot.SidePots.Sum(sp => sp.Amount)
                };

                return new HandEvaluationResult(
                    Winners: new List<Winner> { winner },
                    WinningCards: new List<Card>()
                );
            }

            var winners = new List<Winner>();
            var winningCardsSet = new HashSet<Card>();

            // main pot
            // 1) Minden játékos legjobb 5-öse + kombináció tartása
            var bestMain = new Dictionary<Guid, (PokerHandRank Rank, List<Card> Combo)>();
            foreach (var p in eligible)
            {
                var all = p.HoleCards.Concat(hand.CommunityCards).ToList();
                PokerHandRank? bestRank = null;
                List<Card>? bestCombo = null;

                foreach (var combo in GetAll5CardCombinations(all))
                {
                    var r = EvaluateCombo(combo);
                    if (bestRank is null || r.CompareTo(bestRank) > 0)
                    {
                        bestRank = r;
                        bestCombo = combo;
                    }
                }

                bestMain[p.Id] = (bestRank!, bestCombo!);
            }

            // 2) A legjobb rang
            var topMainRank = bestMain.Values.Max(x => x.Rank);
            var mainPotWinners = bestMain
                .Where(kvp => kvp.Value.Rank.CompareTo(topMainRank) == 0)
                .Select(kvp => kvp.Key)
                .ToList();

            // 3) Osztás
            int mainShare = hand.Pot.MainPot / mainPotWinners.Count;
            foreach (var pid in mainPotWinners)
            {
                var pl = eligible.First(p => p.Id == pid);
                winners.Add(new Winner
                {
                    HandId = hand.Id,
                    PlayerId = pid,
                    Player = pl,
                    Pot = mainShare
                });

                // győztes lapok gyűjtése
                foreach (var c in bestMain[pid].Combo)
                    winningCardsSet.Add(c);
            }

            // side pot
            foreach (var side in hand.Pot.SidePots.OrderBy(sp => sp.Amount))
            {
                // csak ők vesznek részt
                var sideElig = eligible.Where(p => side.EligiblePlayerIds.Contains(p.Id)).ToList();
                if (!sideElig.Any()) continue;

                // legjobb rank+combo
                var bestSide = new Dictionary<Guid, (PokerHandRank, List<Card>)>();
                foreach (var p in sideElig)
                {
                    var all = p.HoleCards.Concat(hand.CommunityCards).ToList();
                    PokerHandRank? br = null;
                    List<Card>? bc = null;
                    foreach (var combo in GetAll5CardCombinations(all))
                    {
                        var r = EvaluateCombo(combo);
                        if (br is null || r.CompareTo(br) > 0)
                        {
                            br = r;
                            bc = combo;
                        }
                    }
                    bestSide[p.Id] = (br!, bc!);
                }

                var topSideRank = bestSide.Values.Max(x => x.Item1);
                var sideWinnersIds = bestSide
                    .Where(kvp => kvp.Value.Item1.CompareTo(topSideRank) == 0)
                    .Select(kvp => kvp.Key)
                    .ToList();

                int sideShare = side.Amount / sideWinnersIds.Count;
                foreach (var pid in sideWinnersIds)
                {
                    var existing = winners.FirstOrDefault(w => w.PlayerId == pid);
                    if (existing != null)
                    {
                        existing.Pot += sideShare;
                    }
                    else
                    {
                        var pl = eligible.First(p => p.Id == pid);
                        winners.Add(new Winner
                        {
                            HandId = hand.Id,
                            PlayerId = pid,
                            Player = pl,
                            Pot = sideShare
                        });
                    }

                    // és a side-combo lapjai is
                    foreach (var c in bestSide[pid].Item2)
                        winningCardsSet.Add(c);
                }
            }

            // végül összerakjuk az eredményt
            return new HandEvaluationResult(
                Winners: winners,
                WinningCards: winningCardsSet.ToList()
            );
        }

        public PokerHandRank EvaluateRank(IEnumerable<Card> holeCards, IEnumerable<Card> communityCards)
        {
            var allCards = holeCards.Concat(communityCards).ToList();
            var combos = GetAll5CardCombinations(allCards);
            PokerHandRank best = null!;
            foreach (var c in combos)
            {
                var rank = EvaluateCombo(c);
                if (best == null || rank.CompareTo(best) > 0)
                    best = rank;
            }
            return best;
        }
        private static IEnumerable<List<Card>> GetAll5CardCombinations(List<Card> cards)
        {
            return GetCombinations(cards, 5);
        }
        private static IEnumerable<List<Card>> GetCombinations(List<Card> cards, int k)
        {
            if (k == 0)
            {
                yield return new List<Card>();
                yield break;
            }
            if (cards.Count < k)
                yield break;

            for (int i = 0; i <= cards.Count - k; i++)
            {
                // A maradék elemekből generáljuk a k-1 kombinációkat
                foreach (var tail in GetCombinations(cards.Skip(i + 1).ToList(), k - 1))
                {
                    var combination = new List<Card> { cards[i] };
                    combination.AddRange(tail);
                    yield return combination;
                }
            }
        }
        private PokerHandRank EvaluateCombo(List<Card> combo)
        {
            if (combo.Count != 5)
                throw new Exception("nem 5");

            var sorted = combo.OrderByDescending(c => (int)c.Rank).ToList();

            bool isFlush = combo.All(c => c.Suit == combo[0].Suit);

            bool isStraight = IsStraight(sorted, out int highStraightRank);

            var rankGroups = combo.GroupBy(c => c.Rank)
                                  .OrderByDescending(g => g.Count())
                                  .ThenByDescending(g => (int)g.Key)
                                  .ToList();

            int maxGroupCount = rankGroups.First().Count();

            var rank = new PokerHandRank();

            if (isFlush && isStraight) // royale, és sor flush
            {
                if (highStraightRank == (int)Rank.Ace)
                {
                    rank.Category = HandCategory.RoyalFlush;
                    rank.Kickers = new List<int> { highStraightRank };
                }
                else
                {
                    rank.Category = HandCategory.StraightFlush;
                    rank.Kickers = new List<int> { highStraightRank };
                }
            }
            else if (maxGroupCount == 4) // négy azonos
            {
                rank.Category = HandCategory.FourOfAKind;
                int fourRank = (int)rankGroups.First().Key;
                int kicker = sorted.Where(c => (int)c.Rank != fourRank).Max(c => (int)c.Rank);
                rank.Kickers = new List<int> { fourRank, kicker };
            }
            else if (maxGroupCount == 3 && rankGroups.Count >= 2 && rankGroups[1].Count() >= 2) // fullhouse
            {
                rank.Category = HandCategory.FullHouse;
                int threeRank = (int)rankGroups.First().Key;
                int pairRank = (int)rankGroups[1].Key;
                rank.Kickers = new List<int> { threeRank, pairRank };
            }
            else if (isFlush) // flush
            {
                rank.Category = HandCategory.Flush;
                rank.Kickers = sorted.Select(c => (int)c.Rank).ToList();
            }
            else if (isStraight) // sor
            {
                rank.Category = HandCategory.Straight;
                rank.Kickers = new List<int> { highStraightRank };
            }
            else if (maxGroupCount == 3) // három azonos
            {
                rank.Category = HandCategory.ThreeOfAKind;
                int threeRank = (int)rankGroups.First().Key;
                var kickers = sorted.Where(c => (int)c.Rank != threeRank)
                                    .Select(c => (int)c.Rank)
                                    .Distinct()
                                    .Take(2)
                                    .ToList();
                rank.Kickers = new List<int> { threeRank };
                rank.Kickers.AddRange(kickers);
            }
            else if (maxGroupCount == 2) // pár
            {
                var pairCount = rankGroups.Count(g => g.Count() == 2);
                if (pairCount >= 2)
                {
                    rank.Category = HandCategory.TwoPair;
                    var pairs = rankGroups.Where(g => g.Count() == 2)
                                          .Select(g => (int)g.Key)
                                          .OrderByDescending(x => x)
                                          .ToList();
                    int kicker = sorted.Where(c => !pairs.Contains((int)c.Rank)).Max(c => (int)c.Rank);
                    // A Kickers első két eleme a két pár, harmadik a kicker
                    rank.Kickers = new List<int> { pairs[0], pairs[1], kicker };
                }
                else
                {
                    rank.Category = HandCategory.OnePair;
                    int pairRank = (int)rankGroups.First().Key;
                    var kickers = sorted.Where(c => (int)c.Rank != pairRank)
                                        .Select(c => (int)c.Rank)
                                        .Distinct()
                                        .Take(3)
                                        .ToList();
                    rank.Kickers = new List<int> { pairRank };
                    rank.Kickers.AddRange(kickers);
                }
            }
            else //magas lap
            {
                rank.Category = HandCategory.HighCard;
                rank.Kickers = sorted.Select(c => (int)c.Rank).ToList();
            }

            return rank;
        }
        private bool IsStraight(List<Card> sortedCards, out int highStraightRank)
        {
            var distinctRanks = sortedCards.Select(c => (int)c.Rank).Distinct().ToList();
            highStraightRank = 0;
            if (distinctRanks.Count < 5) return false;

            for (int i = 0; i <= distinctRanks.Count - 5; i++)
            {
                bool isSeq = true;
                for (int j = 1; j < 5; j++)
                {
                    if (distinctRanks[i] - j != distinctRanks[i + j])
                    {
                        isSeq = false;
                        break;
                    }
                }
                if (isSeq)
                {
                    highStraightRank = distinctRanks[i];
                    return true;
                }
            }

            if (distinctRanks.Contains((int)Rank.Ace) &&
                distinctRanks.Contains(2) &&
                distinctRanks.Contains(3) &&
                distinctRanks.Contains(4) &&
                distinctRanks.Contains(5))
            {
                highStraightRank = 5;
                return true;
            }
            return false;
        }
    }
}
