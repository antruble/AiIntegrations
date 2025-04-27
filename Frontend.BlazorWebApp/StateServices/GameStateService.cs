using Backend.Shared.Models.Poker;
using System.Reflection;

namespace Frontend.BlazorWebApp.StateServices
{
    public class GameStateService
    {
        private readonly ILogger<GameStateService> _logger;
        public GameStateService(ILogger<GameStateService> logger)
        {
            _logger = logger;
        }
        // Az OnChange esemény értesíti a komponenseket a frissítésről.
        public event Action? OnChange;

        // Tárolja az aktuális játék állapotát.
        private GameDto? _currentGame;
        private ICollection<WinnerDto>? _winners;

        private string? _currentHint;
        private double? _playerWinningOdds;
        public string? CurrentHint
        {
            get => _currentHint;
            private set
            {
                _currentHint = value;
                NotifyStateChanged();
            }
        }
        public double? PlayerWinningOdds
        {
            get => _playerWinningOdds;
            private set
            {
                _playerWinningOdds = value;
                NotifyStateChanged();
            }
        }
        public void SetHint(string hint)
        {
            CurrentHint = hint;
        }

        public void ClearHint()
        {
            CurrentHint = null;
        }

        public bool IsLocked { get; set; }
        public GameDto? CurrentGame
        {
            get => _currentGame;
            private set
            {
                var odds = new Dictionary<Guid, double>();
                if (_currentGame?.CurrentHand?.Odds is not null)
                    odds = _currentGame?.CurrentHand?.Odds;

                _currentGame = value;
                
                if(_currentGame!.CurrentHand is not null && _currentGame.CurrentHand.Odds is null && odds?.Count > 0)
                    _currentGame.CurrentHand.Odds = odds;

                NotifyStateChanged();
            }
        }
        public ICollection<WinnerDto>? Winners
        {
            get => _winners;
            private set
            {
                _winners = value;
                NotifyStateChanged();
            }
        }

        public void SetWinners(ICollection<WinnerDto> winners)
        {
            Winners = winners;
        }

        public void ResetWinners()
        {
            Winners = [];
        }
        public void UpdateGame(GameDto newGame) => CurrentGame = newGame;

        // Az esemény kiváltása, ha az állapot változik.
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
