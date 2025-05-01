using Backend.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Domain
{
    public class GameTests
    {
        [Fact]
        public void StartNewHand_ShouldInitializeHandAndAssignBlindsAndChips()
        {
            var initialChipsAmount = 500;
            var players = new[]
            {
                new Player(Guid.NewGuid(), "player1", initialChipsAmount, true, 0),
                new Player(Guid.NewGuid(), "player2", initialChipsAmount, true, 1),
                new Player(Guid.NewGuid(), "player3", initialChipsAmount, true, 2),
            };
            var game = new Game(players.ToList());

            var hand1 = game.StartNewHand();

            Assert.NotNull(hand1);
            Assert.Equal(GameStatus.InProgress, game.Status);
            Assert.Equal(GameActions.DealingCards, game.CurrentGameAction);

            var smallBlindPlayer = game.Players.First(p => p.BlindStatus == BlindStatus.SmallBlind);
            var bigBlindPlayer = game.Players.First(p => p.BlindStatus == BlindStatus.BigBlind);
            Assert.NotEqual(smallBlindPlayer.Id, bigBlindPlayer.Id);

            Assert.Equal(initialChipsAmount - hand1.BigBlindAmount / 2, smallBlindPlayer.Chips);
            Assert.Equal(initialChipsAmount - hand1.BigBlindAmount, bigBlindPlayer.Chips);

            var hand2 = game.StartNewHand();

            Assert.NotEqual(smallBlindPlayer.Id,
                            game.Players.First(p => p.BlindStatus == BlindStatus.SmallBlind).Id);
        }

    }
}
