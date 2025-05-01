using Backend.Application.Services.Poker;
using Backend.Domain.Services;
using Backend.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Application
{
    public class MonteCarloOddsCalculatorTests
    {
        [Fact]
        public void CalculateWinProbabilities_TwoPlayersOneCardEach_ReturnsApproximately50Percent()
        {
            var rankEvaluator = new PokerHandEvaluator();
            var calc = new MonteCarloOddsCalculator(rankEvaluator);

            var player1Id = Guid.NewGuid();
            var player2Id = Guid.NewGuid();

            var holeCards = new Dictionary<Guid, IList<Card>>
            {
                {
                    player1Id,
                    new List<Card>
                    {
                        new Card(Rank.Ace,   Suit.Spades),
                        new Card(Rank.Two,   Suit.Spades)
                    }
                },
                {
                    player2Id,
                    new List<Card>
                    {
                        new Card(Rank.Ace,   Suit.Clubs),
                        new Card(Rank.Two,   Suit.Clubs)
                    }
                }
            };

            var community = new List<Card>();

            var result = calc.CalculateWinProbabilities(holeCards, community, iterations: 1000);

            Assert.Equal(2, result.Count);
            Assert.Contains(player1Id, result.Keys);
            Assert.Contains(player2Id, result.Keys);

            Assert.InRange(result[player1Id], 40.0, 60.0);
            Assert.InRange(result[player2Id], 40.0, 60.0);
        }
    }
}
