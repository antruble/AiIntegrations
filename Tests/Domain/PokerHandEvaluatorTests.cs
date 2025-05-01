using Backend.Domain.Entities;
using Backend.Domain.Services;
using Backend.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Domain
{
    public class PokerHandEvaluatorTests
    {
        private readonly PokerHandEvaluator _evaluator = new();

        [Fact]
        public void Evaluate_SingleRemainingPlayer_ReturnsThatPlayerAsWinner()
        {
            var p1 = new Player(new Guid(), "p1", 100, false, 0);
            var p2 = new Player(new Guid(), "p2", 100, true, 1);

            p1.PlayerStatus = PlayerStatus.Folded;

            var players = new List<Player> { p1, p2 };
            var hand = new Hand(p1);

            hand.DealHoleCards(players);

            var result = _evaluator.Evaluate(hand, players);

            Assert.Single(result.Winners);
            Assert.Equal(p2.Id, result.Winners[0].PlayerId);
            Assert.Empty(result.WinningCards);
        }

        [Fact]
        public void Evaluate_RoyalFlushBeatsStraightFlush()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.Ace, Suit.Hearts),
                    new(Rank.King, Suit.Hearts)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Nine, Suit.Hearts),
                    new(Rank.Eight, Suit.Hearts)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.Queen, Suit.Hearts),
                new Card(Rank.Jack, Suit.Hearts),
                new Card(Rank.Ten, Suit.Hearts),
                new Card(Rank.Seven, Suit.Hearts),
                new Card(Rank.Six, Suit.Hearts)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_StraightFlushBeatsFourOfAKind()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.King, Suit.Hearts),
                    new(Rank.King, Suit.Diamonds)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Two, Suit.Clubs),
                    new(Rank.Three, Suit.Clubs)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.King, Suit.Clubs),
                new Card(Rank.King, Suit.Spades),
                new Card(Rank.Four, Suit.Clubs),
                new Card(Rank.Five, Suit.Clubs),
                new Card(Rank.Six, Suit.Clubs)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p2.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_FourOfAKindBeatsFullHouse()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.King, Suit.Hearts),
                    new(Rank.King, Suit.Diamonds)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Three, Suit.Clubs),
                    new(Rank.Three, Suit.Spades)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.King, Suit.Clubs),
                new Card(Rank.King, Suit.Spades),
                new Card(Rank.Two, Suit.Hearts),
                new Card(Rank.Two, Suit.Diamonds),
                new Card(Rank.Two, Suit.Clubs)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_FullHouseBeatsFlush()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.Three, Suit.Hearts),
                    new(Rank.Three, Suit.Diamonds)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Three, Suit.Spades),
                    new(Rank.Two, Suit.Spades)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.Three, Suit.Clubs),
                new Card(Rank.Four, Suit.Spades),
                new Card(Rank.Four, Suit.Clubs),
                new Card(Rank.Seven, Suit.Spades),
                new Card(Rank.Nine, Suit.Spades)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_FlushBeatsStraight()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.Two, Suit.Hearts),
                    new(Rank.Four, Suit.Hearts)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Ten, Suit.Clubs),
                    new(Rank.Jack, Suit.Diamonds)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.Three, Suit.Hearts),
                new Card(Rank.Six, Suit.Hearts),
                new Card(Rank.Nine, Suit.Hearts),
                new Card(Rank.Eight, Suit.Clubs),
                new Card(Rank.Queen, Suit.Spades)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_StraightBeatsThreeOfAKind()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.Ten, Suit.Clubs),
                    new(Rank.Jack, Suit.Diamonds)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Two, Suit.Hearts),
                    new(Rank.Two, Suit.Spades)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.Seven, Suit.Hearts),
                new Card(Rank.Eight, Suit.Clubs),
                new Card(Rank.Nine, Suit.Spades),
                new Card(Rank.Queen, Suit.Hearts),
                new Card(Rank.Two, Suit.Diamonds)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_ThreeOfAKindBeatsTwoPair()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.Five, Suit.Hearts),
                    new(Rank.Five, Suit.Diamonds)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Six, Suit.Clubs),
                    new(Rank.Seven, Suit.Spades)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.Five, Suit.Clubs),
                new Card(Rank.Two, Suit.Hearts),
                new Card(Rank.Two, Suit.Spades),
                new Card(Rank.Three, Suit.Diamonds),
                new Card(Rank.Four, Suit.Clubs)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_TwoPairBeatsOnePair()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.Ten, Suit.Hearts),
                    new(Rank.Ten, Suit.Diamonds)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Nine, Suit.Clubs),
                    new(Rank.Nine, Suit.Spades)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.Ten, Suit.Clubs),
                new Card(Rank.Nine, Suit.Hearts),
                new Card(Rank.Two, Suit.Diamonds),
                new Card(Rank.Three, Suit.Spades),
                new Card(Rank.Four, Suit.Clubs)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }
        [Fact]
        public void Evaluate_PairBeatsHighCard()
        {
            var p1 = new Player(Guid.NewGuid(), "p1", 100, false, 0,
                new List<Card> {
                    new(Rank.Ten, Suit.Hearts),
                    new(Rank.Ten, Suit.Diamonds)
                });
            var p2 = new Player(Guid.NewGuid(), "p2", 100, true, 1,
                new List<Card> {
                    new(Rank.Ace, Suit.Clubs),
                    new(Rank.Nine, Suit.Spades)
                });

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(new[]
            {
                new Card(Rank.Two, Suit.Clubs),
                new Card(Rank.Jack, Suit.Hearts),
                new Card(Rank.Queen, Suit.Diamonds),
                new Card(Rank.Three, Suit.Spades),
                new Card(Rank.Four, Suit.Clubs)
            });

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

        [Fact]
        public void Evaluate_TwoPlayersHighCard_ComparesCorrectly()
        {
            var p1cards = new List<Card> {
                    new(Rank.Ace, Suit.Spades),
                    new( Rank.King, Suit.Hearts)
            };

            var p1 = new Player(new Guid(), "p1", 100, false, 0, p1cards);


            var p2cards = new List<Card> {
                    new(Rank.Queen, Suit.Diamonds),
                    new( Rank.Jack, Suit.Clubs)
            };

            var p2 = new Player(new Guid(), "p2", 100, true, 0, p2cards);

            var hand = new Hand(p1);
            hand.CommunityCards.AddRange(
                new(Rank.Three, Suit.Diamonds),
                new(Rank.Two, Suit.Diamonds),
                new(Rank.Four, Suit.Diamonds)
                );

            var result = _evaluator.Evaluate(hand, new List<Player> { p1, p2 });

            Assert.Single(result.Winners);
            Assert.Equal(p1.Id, result.Winners[0].PlayerId);
        }

    }
}
