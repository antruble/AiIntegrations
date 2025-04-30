using Backend.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Domain.ValueObjects
{
    public class PokerHandResult
    {
        public List<Guid> WinnerIds { get; set; } = new List<Guid>();
        public Dictionary<Guid, decimal> PotAllocations { get; set; } = new Dictionary<Guid, decimal>();
    }

    public record HandEvaluationResult(
        IList<Winner> Winners,
        IList<Card> WinningCards
    );
}
