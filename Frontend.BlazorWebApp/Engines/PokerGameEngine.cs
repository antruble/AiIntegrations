﻿using Backend.Shared.Models;
using Backend.Shared.Models.Poker;
using Frontend.BlazorWebApp.StateServices;

namespace Frontend.BlazorWebApp.Engines
{
    public class PokerGameEngine : IDisposable
    {
        #region Fields
        private readonly GameStateService _gameStateService;
        private readonly IHttpClientFactory _httpClientFactory;

        private CancellationTokenSource? _gameLoopCts;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isDisposed = false;

        private GameDto _game { get; set; }
        private Guid UserId { get; set; }


        private TaskCompletionSource<PlayerActionDto>? _playerActionTcs;

        private readonly ILogger<PokerGameEngine> _logger;
        private readonly Action _stateHasChangedCallback;
        private bool _isLoading = true;
        private bool _isWaitingForUserAction = false;

        private const double SPEED = 0.2;
        #endregion

        #region Ctor
        public PokerGameEngine(GameDto game, IHttpClientFactory httpClientFactory, ILogger<PokerGameEngine> logger, Action stateHasChangedCallback, GameStateService gameStateService)
        {
            _game = game;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _stateHasChangedCallback = stateHasChangedCallback;
            _gameStateService = gameStateService;
        }

        #endregion

        #region Methods

        #region     Game loop methods

        public void Start()
        {
            if (_gameLoopCts is not null)
                throw new InvalidOperationException("Game loop már fut!");

            _logger.LogInformation($"Engine elindítva..");
            _gameLoopCts = new CancellationTokenSource();
            _gameStateService.IsFinished = false;
            UserId = GetUserId();

            _ = GameLoopAsync(_gameLoopCts.Token);

        }
        public async Task StopAsync()
        {
            if (_gameLoopCts is null) return;
            _gameLoopCts.Cancel();

            try
            {
                await Task.Delay(500);
            }
            finally
            {
                _gameLoopCts.Dispose();
                _gameLoopCts = null;
            }
        }
        private async Task GameLoopAsync(CancellationToken token)
        {
            var interval = TimeSpan.FromSeconds(SPEED * 3); // 3 másodpercenként frissül

            while (!token.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                _logger.LogInformation($"Új loop indult: {startTime}");
                try
                {
                    // Szemafor megakadályozza a párhuzamos futást
                    await _semaphore.WaitAsync(token);
                    if (!_gameStateService.IsLocked)
                    {
                        _gameStateService.IsLocked = true;
                        _logger.LogInformation($"Új beengedett szál: {DateTime.UtcNow} gameaction: {_game.CurrentGameAction}");
                        // Frissítési logika: játék állapot, bot akciók, UI frissítés stb.
                        await UpdateGameStateAsync(token);
                    }
                    else
                    {
                        _logger.LogWarning($"Befutott szál, pedig locked");
                    }

                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("A játékloop megszakítva!");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Kivétel történt a _game loop-ban: {ex.Message}");
                }
                finally
                {
                    // szemafor release
                    if (_semaphore.CurrentCount == 0)
                        _semaphore.Release();
                    _gameStateService.IsLocked = false;
                }

                // Timing
                var elapsed = DateTime.UtcNow - startTime;
                var delay = interval - elapsed;

                if (delay > TimeSpan.Zero)
                {
                    try
                    {
                        await Task.Delay(delay, token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("A játékloop megszakítva várakozás alatt!");
                        break;
                    }
                }
            }

            _logger.LogInformation("Game loop leállt.");
        }
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _semaphore?.Dispose();
            _gameLoopCts?.Dispose();

            _isDisposed = true;
        }
        #endregion

        #region     Game engine methods
        private async Task UpdateGameStateAsync(CancellationToken token)
        {
            if (_game.CurrentHand is null)
                throw new InvalidOperationException("Hiba az UpdateGameStateAsync metódusban: a _game.CurrentHand null!");
            var http = _httpClientFactory.CreateClient("PokerClient");

            if (!_isLoading && !_gameStateService.IsFinished)
            {
                switch (_game.CurrentGameAction)
                {
                    case GameAction.Waiting:
                        _logger.LogInformation($"GameAction is Waiting: ...");
                        break;
                    case GameAction.DealingCards:
                        if (_gameStateService.Winners?.Count != 0)
                            _gameStateService.ResetWinners();

                        _logger.LogInformation($"GameAction is DealingCards: várunk 2 mp-t");
                        await Task.Delay((int)(SPEED * 2000), token);
                        await SendCardsHasDealedStatusAsync(token);
                        break;
                    case GameAction.PlayerAction:
                        _logger.LogInformation($"GameAction is PlayerAction: játékos action generálása és küldése..");
                        await HandlePlayerActionGameActionAsync(token);
                        break;
                    case GameAction.DealNextRound:
                        _logger.LogInformation($"GameAction is PlayerAction: játékos action generálása és küldése..");
                        await DealNextRound(token);
                        break;
                    case GameAction.ShowOff:
                        _logger.LogInformation($"GameAction is ShowOff: várunk 3 mp-t");
                        await Task.Delay((int)(SPEED * 3000), token);

                        var winners = await http.GetFromJsonAsync<ICollection<WinnerDto>>($"getwinners?handid={_game.CurrentHand.Id}") ?? throw new Exception();
                        _gameStateService.SetWinners(winners);
                        _logger.LogInformation($"Vége a handnek, a nyertesek: {string.Join(", ", winners!.Select(p => p.Player.Name))}");
                        await Task.Delay((int)(1 * 4000), token);

                        if (_game.Players.Count(p => p.Chips > 0 && p.IsBot) == 0)
                        {
                            _logger.LogInformation($"Vége a játéknak, mert már csak 1 játékos van waiting státuszban.");
                            _gameStateService.IsLocked = false;
                            _gameStateService.IsFinished = true;
                        }
                        else
                            await http.PostAsync($"startnewhand?gameId={_game.Id}", null, token);
                        break;
                    default:
                        break;
                }

            }

            _isLoading = false;
            // Játék frissítése
            await UpdateGameAsync(token);

        }
        private async Task DealNextRound(CancellationToken token)
        {
            var http = _httpClientFactory.CreateClient("PokerClient");

            var response = await http.PostAsync(
                $"dealnextround?gameId={_game.Id}",
                content: null,
                cancellationToken: token
            );

            response.EnsureSuccessStatusCode();

            var game = await response.Content.ReadFromJsonAsync<GameDto>(cancellationToken: token)
                ?? throw new InvalidOperationException("Null a game a DealNextRound-ban");

            _gameStateService.UpdateGame(game);
            await Task.Delay((int)(SPEED * 1000));
        }
        private async Task SendCardsHasDealedStatusAsync(CancellationToken token)
        {
            var http = _httpClientFactory.CreateClient("PokerClient");
            await http.PostAsync($"cardshasdealed?gameId={_game.Id}", null, token);
        }
        private async Task HandlePlayerActionGameActionAsync(CancellationToken token)
        {
            var http = _httpClientFactory.CreateClient("PokerClient");
            var currentPlayer = _game.Players!.First(p => p.Id == _game.CurrentHand!.CurrentPlayerId);

            if (currentPlayer is null)
            {
                _logger.LogWarning($"Nem jön senki");
                return;
            }

            if (currentPlayer.PlayerStatus == PlayerStatus.Folded || currentPlayer.PlayerStatus == PlayerStatus.AllIn)
            {
                _logger.LogError($"Error: a {currentPlayer.Name} játékos jött, pedig {currentPlayer.PlayerStatus} a státusza");
                return;
            }
            _logger.LogInformation($"PlayerAction: a soron következő játékos: {currentPlayer.Name}");

            if (currentPlayer.IsBot)
            {
                _logger.LogInformation($"PlayerAction: mivel a soron következő játékos bot, ezért várunk 2mp-t..");
                await Task.Delay((int)(SPEED * 2000), token);
            }

            if (currentPlayer.IsBot)
                await HandleBotActionsAsync(currentPlayer, token);
            else
                await HandleUserActionsAsync(token);

        }
        private async Task HandleUserActionsAsync(CancellationToken token)
        {
            _logger.LogWarning("1.1 LEFUTOTT A USER ACTION");
            if (_isWaitingForUserAction)
            {
                _logger.LogWarning("1.1.1 Nincs szükség új várakozásra, mert már folyamatban van a várakozás.");
                return;
            }
            _isWaitingForUserAction = true;

            try
            {
                if (_playerActionTcs == null)
                    _playerActionTcs = new TaskCompletionSource<PlayerActionDto>();

                _logger.LogWarning("1.2 Várakozás elkezdve...");
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(300), token);

                var completedTask = await Task.WhenAny(_playerActionTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning($"1.5.b Timeout történt, default Fold lesz alkalmazva. ThreadID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    _playerActionTcs.TrySetResult(new PlayerActionDto(PlayerActionType.Fold, null, DateTime.UtcNow));
                }
                else
                {
                    _logger.LogWarning("1.5.a Játékos action érkezett.");
                }

                var action = await _playerActionTcs.Task;
                _logger.LogWarning($"1.6 MEGVAN AZ ACTION, AMI: {action.ActionType}");
                _logger.LogInformation($"PlayerAction: a játékos action-je: {action.ActionType} érték: {action.Amount}");
                var http = _httpClientFactory.CreateClient("PokerClient");
                await http.PostAsJsonAsync($"processaction?gameId={_game.Id}&playerId={UserId}", action, token);
                _gameStateService.ClearHint();


            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in HandleUserActionsAsync: {ex.Message}");
            }
            finally
            {
                _playerActionTcs = null;
                _isWaitingForUserAction = false;
            }
        }

        private async Task HandleBotActionsAsync(PlayerDto bot, CancellationToken token)
        {
            var http = _httpClientFactory.CreateClient("PokerClient");
            try
            {
                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: {bot.Name} bot action generálása elkezdődőtt..");

                var action = await http.GetFromJsonAsync<PlayerActionDto>($"generatebotaction?gameId={_game.Id}&botId={bot.Id}", token)
                        ?? throw new InvalidOperationException("Error.PlayerAction.HandlePlayerActionGameActionAsync: a generált action null");

                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: a generált action: {action.ActionType} érték: {action.Amount}. Action küldése a szervernek feldolgozásra..");

                await http.PostAsJsonAsync($"processaction?gameId={_game.Id}&playerId={bot.Id}", action, token);
                _logger.LogInformation($"PlayerAction.HandlePlayerActionGameActionAsync: generált action feldolgozva");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error.PlayerAction.HandleBotActionsAsync: {ex.Message}");
                return;
            }
        }
        public void RecordPlayerAction(PlayerActionDto action) => _playerActionTcs?.TrySetResult(action);

        public async Task RequestHintAsync()
        {
            _gameStateService.ClearHint();

            var client = _httpClientFactory.CreateClient("PokerClient");

            double oddsValue = 0.0;
            if (_game.CurrentHand?.Odds != null
                && _game.CurrentHand.Odds.TryGetValue(UserId, out var retrievedOdds))
            {
                oddsValue = retrievedOdds;
            }

            var url = $"gethint?gameId={_game.Id}&playerId={UserId}&odds={oddsValue}";
            _logger.LogInformation($"Hint kérése: {url}");

            var hintResp = await client.GetFromJsonAsync<HintResponse>(url)
                ?? throw new InvalidOperationException("HintResponse deserializálása sikertelen.");

            _gameStateService.SetHint(hintResp.Advice);
        }


        private async Task UpdateGameAsync(CancellationToken token)
        {
            var http = _httpClientFactory.CreateClient("PokerClient");
            _game = await http
                    .GetFromJsonAsync<GameDto>($"getgamebyid/{_game.Id}", token)
                    ?? throw new InvalidOperationException($"Null a game a UpdateGameStateAsync-ban");

            _gameStateService.UpdateGame(_game);
            _stateHasChangedCallback?.Invoke();
        }
        #endregion

        #region     Utilities
        public Guid GetCurrentPlayersId() =>
                _game.CurrentHand!.CurrentPlayerId;

        public Guid GetUserId()
        {
            if (_game.Players == null || !_game.Players.Any())
            {
                var client = _httpClientFactory.CreateClient("PokerClient");
                var game = client
                    .GetFromJsonAsync<GameDto>($"getgamebyid/{_game.Id}")
                    .GetAwaiter().GetResult();

                if (game == null || game.Players == null || !game.Players.Any())
                    throw new InvalidOperationException($"Nem található játék vagy játékosok a szerveren. gameId={_game.Id}");
            }

            var user = _game.Players.FirstOrDefault(p => !p.IsBot)
                       ?? throw new InvalidOperationException("Nincs emberi játékos a játékban.");
            return user.Id;
        }

        #endregion



        #endregion
    }
}
