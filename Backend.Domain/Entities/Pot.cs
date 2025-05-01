using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Backend.Domain.Entities
{
    public class Pot
    {
        public int MainPot { get; private set; }
        public int CurrentRoundPot { get; private set; }
        public List<PlayerContribution> Contributions { get; private set; } = new();
        public List<SidePot> SidePots { get; private set; } = new();


        [JsonConstructor]
        public Pot(int mainPot, int currentRoundPot, List<PlayerContribution> contributions, List<SidePot> sidePots)
        {
            MainPot = mainPot;
            CurrentRoundPot = currentRoundPot;
            Contributions = contributions;
            SidePots = sidePots;
        }
        public Pot() { }
        public void AddContribution(Guid playerId, int amount)
        {
            int index = Contributions.FindIndex(pc => pc.PlayerId == playerId);
            if (index >= 0)
                Contributions[index] = Contributions[index].Add(amount);
            else
                Contributions.Add(new PlayerContribution(playerId, amount));
            CurrentRoundPot += amount;
        }
        public int GetCallAmountForPlayer(Guid playerId)
        {
            int playerContribution = Contributions.FirstOrDefault(pc => pc.PlayerId == playerId)?.Amount ?? 0;

            int maxContribution = Contributions.Any() ? Contributions.Max(pc => pc.Amount) : 0;

            int callAmount = maxContribution - playerContribution;

            if (callAmount < 0)
                throw new Exception("A call értéke nem lehet kisebb mint 0");

            return callAmount;
        }
        public void CompleteRound()
        {
            MainPot += CurrentRoundPot;
            CurrentRoundPot = 0;
        }

        public void AddSidePot(int amount, List<Guid> eligiblePlayers)
        {
            SidePots.Add(new SidePot(amount, eligiblePlayers));
        }

        public void CreateSidePots()
        {
            var sortedContributions = Contributions.OrderBy(pc => pc.Amount).ToList();
            if (sortedContributions.Count == 0)
                return;
            
            int baseAmount = sortedContributions.First().Amount;

            var extraContributions = sortedContributions
                .Select(pc => new { pc.PlayerId, Extra = pc.Amount - baseAmount })
                .ToList();

            var eligibleForSidePot = extraContributions.Where(x => x.Extra > 0).ToList();
            if (!eligibleForSidePot.Any())
                return;

            int sidePotBase = eligibleForSidePot.Min(x => x.Extra);
            int sidePotAmount = sidePotBase * eligibleForSidePot.Count;
            AddSidePot(sidePotAmount, eligibleForSidePot.Select(x => x.PlayerId).ToList());

            for (int i = 0; i < eligibleForSidePot.Count; i++)
            {
                eligibleForSidePot[i] = new
                {
                    eligibleForSidePot[i].PlayerId,
                    Extra = eligibleForSidePot[i].Extra - sidePotBase
                };
            }

            while (eligibleForSidePot.Any(x => x.Extra > 0))
            {
                var currentEligible = eligibleForSidePot.Where(x => x.Extra > 0).ToList();
                int currentBase = currentEligible.Min(x => x.Extra);
                int currentSidePot = currentBase * currentEligible.Count;
                AddSidePot(currentSidePot, currentEligible.Select(x => x.PlayerId).ToList());
                eligibleForSidePot = currentEligible
                    .Select(x => new { x.PlayerId, Extra = x.Extra - currentBase })
                    .ToList();
            }
        }
    }

    public record PlayerContribution(Guid PlayerId, int Amount)
    {
        public PlayerContribution Add(int additionalAmount) => new(PlayerId, Amount + additionalAmount);
    }

    public record SidePot(int Amount, List<Guid> EligiblePlayerIds);
}
