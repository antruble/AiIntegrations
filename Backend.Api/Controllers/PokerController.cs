using Backend.Application.Interfaces.Poker;
using Backend.Domain.Entities;
using Backend.Shared.Models;
using Backend.Shared.Models.Poker;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PokerController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IPlayerService _playerService;
        private readonly IBotService _botService;
        private readonly IHintService _hintService;
        private readonly ILogger<PokerController> _logger;

        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gameLocks = new();

        public PokerController(
            IGameService gameService,
            IPlayerService playerService,
            IBotService botService,
            IHintService hintService,
            ILogger<PokerController> logger
            )
        {
            _gameService = gameService;
            _playerService = playerService;
            _botService = botService;
            _hintService = hintService;
            _logger = logger;
        }

        [HttpPost("newgame")]
        public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request is null.");
            }

            var game = await _gameService.StartNewGameAsync(request.NumOfBots, request.PlayerName);
            return Ok(game.Id);
        }

        [HttpGet("getgamebyid/{gameId:guid}")]
        public async Task<IActionResult> GetGameById(Guid gameId)
        {
            var game = await _gameService.GetGameByIdAsync(gameId);
            if (game is null)
            {
                return BadRequest($"Nem található game az adott game id-vel. Game ID: {gameId}");
            }
            return Ok(game);
        }

        [HttpGet("getgame")]
        public async Task<IActionResult> GetGame()
        {
            var game = await _gameService.GetGameAsync();

            if (game is null)
                return NoContent();

            return Ok(game);
        }
        [HttpGet("getuserid")]
        public async Task<IActionResult> GetUserId(Guid gameId)
        {
            var playerId = await _playerService.GetUserIdAsync(gameId);
            return Ok(playerId);
        }
        [HttpGet("generatebotaction")]
        public async Task<IActionResult> GenerateBotAction(Guid gameId, Guid botId)
        {
            try
            {
                var game = await _gameService.GetGameByIdAsync(gameId)
                    ?? throw new InvalidOperationException($"Nem található game {gameId} ID-val");
                var callAmount = game.CurrentHand!.Pot.GetCallAmountForPlayer(botId);
                var action = await _botService.GenerateBotActionAsync(botId, callAmount);
                return Ok(action);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GenerateBotAction.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("processaction")]
        public async Task<IActionResult> ProcessActionAsync(Guid gameId, Guid playerId, [FromBody] PlayerAction action)
        {

            try
            {
                await ProcessGameRequestAsync(gameId, async () =>
                {
                    _logger.LogInformation($"Új szál beengedve a ProcessActionAsync belsejébe. ThreadID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    var game = await _gameService.GetGameByIdAsync(gameId)
                        ?? throw new InvalidOperationException($"Nem található game {gameId} ID-val");

                    if (game.CurrentGameAction != GameActions.PlayerAction)
                        throw new InvalidOperationException($"A game action-ja nem ProcessAction, hanem {game.CurrentGameAction}");

                    var player = await _playerService.GetPlayerByIdAsync(playerId)
                        ?? throw new InvalidOperationException($"Nem található player {playerId} ID-val");

                    await _gameService.ProcessActionAsync(game, player, action);
                });

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ProcessActionAsync.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("cardshasdealed")]
        public async Task<IActionResult> CardsHasDealedAsync(Guid gameId)
        {
            try
            {
                await ProcessGameRequestAsync(gameId, async () =>
                {
                    _logger.LogInformation($"Kártyák kiosztva, gameaction továbbléptetése");
                    await _gameService.CardsDealingActionFinished(gameId);
                });
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"CardsHasDealedAsync.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("startnewhand")]
        public async Task<IActionResult> StartNewHandAsync(Guid gameId)
        {
            try
            {
                await ProcessGameRequestAsync(gameId, async () =>
                {
                    await _gameService.StartNewHandAsync(gameId);
                });
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"StartNewHandAsync.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("dealnextround")]
        public async Task<IActionResult> DealNextRound(Guid gameId)
        {
            try
            {
                await ProcessGameRequestAsync(gameId, async () =>
                {
                    var game = await _gameService.GetGameByIdAsync(gameId) ?? throw new Exception($"Nem található game {gameId} ID-val!");
                    if (game.CurrentHand!.HandStatus == Domain.Entities.HandStatus.Shutdown)
                        game = await _gameService.SetGameActionShowOff(game);
                    else
                        game = await _gameService.DealNextRound(game);
                });
                var updatedGame = await _gameService.GetGameByIdAsync(gameId);
                return Ok(updatedGame);
            }
            catch (Exception ex)
            {
                _logger.LogError($"DealNextRound.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getwinners")]
        public async Task<IActionResult> GetWinners(Guid handId)
        {
            try
            {
                var winners = await _gameService.GetWinners(handId);
                return Ok(winners);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetWinners.Error: {ex.Message}", ex);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("gethint")]
        public async Task<IActionResult> GetHint([FromQuery] Guid gameId, [FromQuery] Guid playerId, [FromQuery] double odds)
        {
            // 1) Lekérjük a játékot a service-ből
            var game = await _gameService.GetGameByIdAsync(gameId);
            if (game == null)
                return NotFound($"Nincs ilyen game: {gameId}");

            // 2) Megkeressük a kérő játékost
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            if (player == null)
                return BadRequest($"A playerId ({playerId}) nem egyezik a játékosok egyikével sem.");

            // 3) Domain ValueObject → DTO mapping
            var communityDtos = game.CurrentHand?.CommunityCards
                .Select(c => new CardDto(
                     Rank: (RankDto)c.Rank,
                     Suit: (SuitDto)c.Suit,
                     DisplayValue: c.DisplayValue
                ))
                .ToList()
                ?? new List<CardDto>();

            var holeDtos = player.HoleCards
                .Select(c => new CardDto(
                     Rank: (RankDto)c.Rank,
                     Suit: (SuitDto)c.Suit,
                     DisplayValue: c.DisplayValue
                ))
                .ToList();

            var callAmount = _gameService.GetCallAmountForPlayer(game, player);

            // 3) Összeállítjuk a HintRequest-et
            var hintReq = new HintRequest(
                GameId: game.Id,
                PlayerId: playerId,
                CommunityCards: communityDtos,
                HoleCards: holeDtos,
                WinProbability: odds,
                Budget: player.Chips,
                CallAmount: callAmount
            );

            // 4) Meghívjuk a hint use-case-et
            var hintResp = await _hintService.GetHintAsync(hintReq);

            return Ok(hintResp);
        }

        #region Gamelocks
        private SemaphoreSlim GetGameLock(Guid gameId)
        {
            return _gameLocks.GetOrAdd(gameId, _ => new SemaphoreSlim(1, 1));
        }

        public async Task ProcessGameRequestAsync(Guid gameId, Func<Task> action)
        {
            var gameLock = GetGameLock(gameId);
            await gameLock.WaitAsync();
            try
            {
                _logger.LogWarning($"ThreadID ami most lett ELINDÍTVA: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                await action();
            }
            finally
            {
                _logger.LogWarning($"ThreadID ami fog LEZÁRULNI: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                gameLock.Release();
            }
        }
        #endregion
    }
}
