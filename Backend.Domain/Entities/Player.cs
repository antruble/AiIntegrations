using Backend.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Backend.Domain.Entities
{
    public class Player
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public int Chips { get; private set; }
        public bool IsBot { get; private set; }
        public int Seat { get; set; }
        public bool HasToRevealCards { get; set; } = false;
        public List<Card> HoleCards { get; private set; }
        public BlindStatus BlindStatus { get; set; }
        public PlayerStatus PlayerStatus { get; set; }

        // AI training szempontjából érdemes logolni a játékos döntéseit
        public List<PlayerAction> ActionsHistory { get; private set; }

        protected Player() { }

        public Player(Guid id, string name, int chips, bool isBot, int seat)
        {
            Id = id;
            Name = name;
            Chips = chips;
            IsBot = isBot;
            HoleCards = new List<Card>();
            ActionsHistory = new List<PlayerAction>();
            Seat = seat;

            BlindStatus = BlindStatus.None;
            PlayerStatus = PlayerStatus.Waiting;
        }
        //konstruktor tesztelésheu
        public Player(Guid id, string name, int chips, bool isBot, int seat, List<Card> holeCards)
        {
            Id = id;
            Name = name;
            Chips = chips;
            IsBot = isBot;
            HoleCards = holeCards;
            ActionsHistory = new List<PlayerAction>();
            Seat = seat;

            BlindStatus = BlindStatus.None;
            PlayerStatus = PlayerStatus.Waiting;
        }

        [JsonConstructor]
        public Player(Guid id, string name, int chips, bool isBot, int seat, List<Card> holeCards, List<PlayerAction> actionsHistory)
            => (Id, Name, Chips, IsBot, Seat, HoleCards, ActionsHistory) = (id, name, chips, isBot, seat, holeCards, actionsHistory);


        public void DeductChips(int amount)
        {
            if (amount >= Chips)
            {
                amount = Chips;
                PlayerStatus = PlayerStatus.AllIn;
            }

            AddActionToHistory(new PlayerAction(PlayerActionType.Call, amount));
            Chips -= amount;
        }

        public void AddActionToHistory(PlayerAction action)
            => ActionsHistory.Add(action);

        public void AddChips(int amount) => Chips += amount;
        public void ResetHoleCards() => HoleCards = new List<Card>();
        public void ResetChips() => Chips = 2000;
        public void ResetPlayerAttributes()
        {
            BlindStatus = BlindStatus.None;
            if (PlayerStatus != PlayerStatus.Lost)
                PlayerStatus = PlayerStatus.Waiting;

            ResetHoleCards();
        }
        public void Fold()
        {
            PlayerStatus = PlayerStatus.Folded;
            AddActionToHistory(new PlayerAction(PlayerActionType.Fold, 0));
        }
    }

    public enum PlayerActionType
    {
        Fold,
        Call,
        Raise,
        Check
    }
    public enum PlayerStatus
    {
        Lost,
        Folded,
        Waiting,
        AllIn
    }
    public enum BlindStatus
    {
        BigBlind,
        SmallBlind,
        None,
    }

    public class PlayerAction
    {
        public PlayerAction() { }

        public PlayerAction(PlayerActionType actionType, int? amount, DateTime timestamp)
        {
            ActionType = actionType;
            Amount = amount;
            Timestamp = timestamp;
        }
        public PlayerAction(PlayerActionType actionType, int? amount)
        {
            ActionType = actionType;
            Amount = amount;
            Timestamp = DateTime.UtcNow;
        }

        public PlayerActionType ActionType { get; set; }
        public int? Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
