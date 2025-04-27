using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared.Models.Poker
{
    // Enums
    public enum GameStatus { NotStarted, InProgress, Completed }
    public enum GameAction { Waiting, DealingCards, PlayerAction, DealNextRound, ShowOff }
    public enum HandStatus { Preflop, Flop, Turn, River, Shutdown }
    public enum PlayerActionType { Fold, Call, Raise, Check }
    public enum PlayerStatus { Lost, Folded, Waiting, AllIn }
    public enum BlindStatus { BigBlind, SmallBlind, None }

    public record PlayerActionDto(
        PlayerActionType ActionType,
        int? Amount,
        DateTime Timestamp
    );

    public record PlayerDto(
        Guid Id,
        string Name,
        int Chips,
        bool IsBot,
        int Seat,
        bool HasToRevealCards,
        List<CardDto> HoleCards,
        BlindStatus BlindStatus,
        PlayerStatus PlayerStatus,
        List<PlayerActionDto> ActionsHistory
    );

    public record PlayerContributionDto(Guid PlayerId, int Amount);
    public record SidePotDto(int Amount, List<Guid> EligiblePlayerIds);

    public record PotDto(
        int MainPot,
        int CurrentRoundPot,
        List<PlayerContributionDto> Contributions,
        List<SidePotDto> SidePots
    );

    public record HandDto(
        Guid Id,
        List<CardDto> CommunityCards,
        HandStatus HandStatus,
        PotDto Pot,
        Guid FirstPlayerId,
        Guid PivotPlayerId,
        Guid CurrentPlayerId,
        bool SkipActions
    )
    {
        public Dictionary<Guid, double> Odds { get; set; } = new();
    }

    public record GameDto(
        Guid Id,
        List<PlayerDto> Players,
        HandDto? CurrentHand,
        GameStatus Status,
        DateTime CreatedOnUtc,
        GameAction CurrentGameAction
    );

    public record WinnerDto(
        Guid HandId,
        Guid PlayerId,
        PlayerDto Player,
        int Pot
    );

    #region Value objects
    public enum SuitDto { Clubs, Diamonds, Hearts, Spades }
    public enum RankDto
    {
        Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
        Jack, Queen, King, Ace
    }
    public record CardDto(RankDto Rank, SuitDto Suit, string DisplayValue);
    public record PokerHandResultDto(
        List<Guid> WinnerIds,
        Dictionary<Guid, decimal> PotAllocations
    );

    #endregion
}
