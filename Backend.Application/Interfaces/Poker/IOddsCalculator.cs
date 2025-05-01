using Backend.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces.Poker
{
    public interface IOddsCalculator
    {
        Dictionary<Guid, double> CalculateWinProbabilities(
            Dictionary<Guid, IList<Card>> holeCards,
            IList<Card> communityCards,
            int iterations = 10_000);
    }
}
