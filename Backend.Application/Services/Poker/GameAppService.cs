﻿using Backend.Application.Interfaces.Poker;
using Backend.Domain.Entities;
using Backend.Domain.IRepositories;
using Backend.Domain.Services;
using Backend.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Services.Poker
{
    public class GameAppService : IGameService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPlayerService _playerService;
        private readonly IPokerHandEvaluator _handEvaluator;
        private readonly IOddsCalculator _oddsCalculator;
        private readonly ILogger<GameAppService> _logger;

        private const string DeckMemoryCacheKey = "DECK-CACHE-KEY-";
        private readonly IMemoryCache _cache;

        public GameAppService(
            IUnitOfWork unitOfWork,
            IPlayerService playerService,
            IPokerHandEvaluator handEvaluator,
            IOddsCalculator oddsCalculator,
            ILogger<GameAppService> logger,

            IMemoryCache cache
            )
        {
            _unitOfWork = unitOfWork;
            _playerService = playerService;
            _handEvaluator = handEvaluator;
            _oddsCalculator = oddsCalculator;
            _logger = logger;

            _cache = cache;
        }

        public async Task<Game> GetGameAsync()
        {
            try
            {
                var games = await _unitOfWork.Games.GetAllAsync(filter: g => g.Status != GameStatus.Completed, includeProperties: "Players,CurrentHand");

                var game =
                    games.ToList().OrderBy(g => g.Id).FirstOrDefault()
                    ?? await StartNewGameAsync(5);

                game.Players = [.. game.Players.OrderBy(p => p.Seat)];

                return game;
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        public async Task<Game?> GetGameByIdAsync(Guid gameId)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(gameId);
            if (game is not null)
                game.Players = game.Players.OrderBy(p => p.Seat).ToList();

            return game;
        }
        public async Task<Game> StartNewGameAsync(int numOfBots, string playerName = "Player")
        {
            var players = await _playerService.GetPlayersAsync(numOfBots, playerName);
            Game game = new(players);
            game.StartNewHand();

            await _unitOfWork.Games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();

            return game;
        }
        public async Task CardsDealingActionFinished(Guid gameId)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(gameId)
                       ?? throw new Exception($"Nem található játék az alábbi azonosítóval: {gameId}");
            if (!game.CurrentHand!.SkipActions)
                game.CurrentGameAction = GameActions.PlayerAction;
            else
                game.CurrentGameAction = GameActions.DealNextRound;
            await _unitOfWork.Games.SaveChangesAsync();
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task<Game> ProcessActionAsync(Game game, Player player, PlayerAction action)
        {
            _logger.LogInformation($"LEFUTOTT A ProcessActionAsync");
            if (game.GetCurrentPlayersId() != player.Id)
                throw new Exception("Error.ProcessActionAsync: nem a soron következő játékos hívta meg a függvényt. függvényt meghívó játékos: {player.Id} jelenlegi játékos: {game.GetCurrentPlayersId()} ");
            if (game.CurrentHand!.SkipActions)
                throw new Exception($"Error.ProcessActionAsync: a skip actions nem kéne, hogy true legyen");

            await HandleActionAsync(game, player, action);
            if (game.CurrentGameAction != GameActions.ShowOff)
            {
                // bezárölt kör -> vagy új kör, vagy vége a hand
                if (game.IsNextPlayerPivot())
                {
                    game.CurrentGameAction = GameActions.DealNextRound;
                }
                else // még nem zárult be a kör
                {
                    game.CurrentGameAction = GameActions.PlayerAction;
                    game.SwitchToTheNextPlayer(lastPlayer: player);
                }
                _logger.LogInformation($"Átváltva a következő játékosra (ha nem hand vége volt)");
            }

            await _unitOfWork.SaveChangesAsync();
            return game;
        }
        public async Task<Game> DealNextRound(Game game)
        {
            foreach (var player in game.Players)
                player.ActionsHistory.Clear();

            if (game.CurrentHand!.HandStatus == HandStatus.River)
            {
                await ProcessFinishedHandAsync(game);
            }
            else
            {
                if (game.Players.Count(p => p.PlayerStatus == PlayerStatus.Waiting) < 2)
                    game.CurrentHand!.SkipActions = true;
                _logger.LogInformation($"Bezárult az előző kör, új kört osztunk. Előző kör: {game.CurrentHand!.HandStatus}");

                var deck = GetCurrentDeck(game);
                game.CurrentHand.DealNextRound(deck!);
                game.CurrentHand.Pot.CompleteRound();
                game.CurrentGameAction = GameActions.DealingCards;
                if (!game.CurrentHand.SkipActions)
                {
                    _logger.LogInformation($"A következő játékos a pivot játékos, de még nincs river, ezért a körnek vége");
                    var hole = game.Players.ToDictionary(p => p.Id, p => (IList<Card>)p.HoleCards);
                    var community = game.CurrentHand.CommunityCards;

                    var odds = _oddsCalculator.CalculateWinProbabilities(hole, community);
                    game.CurrentHand.Odds = odds;

                    _logger.LogInformation("Kör végi odds-értékek elmentve.");

                    game.Players
                        .Where(p => p.PlayerStatus == PlayerStatus.AllIn)
                        .ToList()
                        .ForEach(p => p.HasToRevealCards = true);

                    game.CurrentGameAction = GameActions.DealingCards;

                    var playerId = game.SetRoundsFirstPlayerToCurrent();
                    game.SetCurrentPlayerToPivot(playerId);
                }

            }
            await _unitOfWork.Games.UpdateAsync(game);
            await _unitOfWork.SaveChangesAsync();

            return game;

        }
        public async Task ProcessFinishedHandAsync(Game game)
        {
            _logger.LogInformation($"A következő játékos a pivot játékos, és river van, ezért a handnek vége");
            game.CurrentHand!.Pot.CreateSidePots();

            await CompleteHandAndSaveWinnersAsync(game);
            await EliminateChiplessPlayersAsync(game);

            game.CurrentGameAction = GameActions.ShowOff;
        }
        public async Task<Game> SetGameActionShowOff(Game game)
        {
            game.CurrentGameAction = GameActions.ShowOff;
            await _unitOfWork.Games.UpdateAsync(game);
            await _unitOfWork.Games.SaveChangesAsync();

            return game;
        }
        private async Task CompleteHandAndSaveWinnersAsync(Game game)
        {
            try
            {
                var result = game.CurrentHand!.CompleteHand(_handEvaluator, game.Players);

                foreach (var card in result.WinningCards)
                {
                    card.IsHighlighted = true;
                }

                result.Winners
                    .Join(game.Players,
                          winner => winner.PlayerId,
                          player => player.Id,
                          (winner, player) => new { Winner = winner, Player = player })
                    .ToList()
                    .ForEach(x => x.Player.AddChips(x.Winner.Pot));

                _logger.LogInformation(
                    "Winners: {WinnerNames}",
                    string.Join(", ", result.Winners.Select(w => w.Player.Name))
                );

                _logger.LogInformation($"WinnersCount: {result.Winners.Count}");
                await _unitOfWork.Winners.AddRangeAsync(result.Winners.ToList());
                await _unitOfWork.Winners.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error a hand befejezése és mentése közben: {ex.Message}", ex);
                throw;
            }

        }

        private async Task EliminateChiplessPlayersAsync(Game game)
        {
            // Nem-bot játékosok esetében, ha chipje <= 0, akkor visszaállítjuk 1000-re
            foreach (var player in game.Players.Where(p => !p.IsBot && p.Chips <= 0).ToList())
                player.AddChips(1000);

            game.Players = game.Players.Where(p => p.Chips > 0).ToList();

            await _unitOfWork.Players.SaveChangesAsync();
        }

        public async Task StartNewHandAsync(Guid gameId)
        {
            var game = await _unitOfWork.Games.GetByIdAsync(gameId)
                        ?? throw new NullReferenceException($"Nem található game a megadott game ID-val. Game ID: {gameId}");

            var activePlayersCount = game.Players
                .Where(p => p.PlayerStatus != PlayerStatus.Lost).Count();
            if (activePlayersCount < 2)
                throw new Exception("Nem lehet új handet indítani, mert kevesebb mint 2 aktív játékos van.");

            var hand = game.StartNewHand();

            game.CurrentGameAction = GameActions.DealingCards;

            await _unitOfWork.Hands.AddAsync(hand);
            await _unitOfWork.Games.SaveChangesAsync();
        }
        public async Task<List<Winner>> GetWinners(Guid handId)
        {
            var winners = await _unitOfWork.Winners.GetAllAsync(filter: w => w.HandId == handId);

            foreach (var winner in winners)
            {
                if (winner is null)
                    continue;

                winner.Player = await _unitOfWork.Players.GetByIdAsync(winner.PlayerId)
                                    ?? throw new NullReferenceException($"Nem találtam a playert");
            }

            return winners.ToList();
        }
        private Deck GetCurrentDeck(Game game)
        {
            if (!_cache.TryGetValue($"{DeckMemoryCacheKey}{game.CurrentHand!.Id}", out Deck? deck))
            {
                var drawnCards = game.Players.SelectMany(p => p.HoleCards).ToList();
                deck = game.CurrentHand.RestoreDeck(drawnCards) ?? throw new Exception("Nem lehet null ez itt");

                var cacheOptions = new MemoryCacheEntryOptions()
                                        .SetSlidingExpiration(TimeSpan.FromSeconds(120))
                                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set($"{DeckMemoryCacheKey}{game.CurrentHand.Id}", deck, cacheOptions);
            }
            return deck;
        }

        private async Task HandleActionAsync(Game game, Player player, PlayerAction action)
        {
            switch (action.ActionType)
            {
                case PlayerActionType.Fold:
                    player.Fold();
                    var nonFoldedPlayerCount = game.Players.Count(p => p.PlayerStatus == PlayerStatus.Waiting || p.PlayerStatus == PlayerStatus.AllIn);
                    if (nonFoldedPlayerCount == 1)
                    {
                        await CompleteHandAndSaveWinnersAsync(game);
                        game.CurrentGameAction = GameActions.ShowOff;
                        break;
                    }

                    if (!game.Players.Any(p => p.Seat > player.Seat && p.PlayerStatus == PlayerStatus.Waiting))
                    {
                        var waitingPlayerCount = game.Players.Count(p => p.PlayerStatus == PlayerStatus.Waiting);
                        if (waitingPlayerCount <= 1)
                            game.CurrentHand!.SkipActions = true;
                    }

                    break;
                case PlayerActionType.Call:
                    var amount = game.CurrentHand!.Pot.GetCallAmountForPlayer(player.Id);

                    if (amount == 0)
                    {
                        action.ActionType = PlayerActionType.Check;
                        player.AddActionToHistory(new PlayerAction(PlayerActionType.Check, 0));
                    }
                    else
                    {
                        game.DeductPlayerChips(player, amount);
                    }

                    break;
                case PlayerActionType.Raise:
                    if (action.Amount is null || action.Amount <= 0)
                        throw new Exception("Raise volt null, vagy kisebb egyenlő 0 értékű amounttal");

                    HandleRaiseAction(game, player, (int)action.Amount);
                    break;
            }
        }
        public int GetCallAmountForPlayer(Game game, Player player)
            => game.CurrentHand!.Pot.GetCallAmountForPlayer(player.Id);
        private void HandleRaiseAction(Game game, Player player, int amount)
        {
            var callAmount = game.CurrentHand!.Pot.GetCallAmountForPlayer(player.Id);
            if (callAmount > amount)
                amount = callAmount + 1;

            game.DeductPlayerChips(player, amount);
            game.SetCurrentPlayerToPivot(player.Id);
        }
    }
}
