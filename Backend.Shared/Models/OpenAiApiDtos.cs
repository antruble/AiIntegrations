using Backend.Shared.Models.Poker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared.Models
{

    public record HintRequest(
        Guid GameId,
        Guid PlayerId,
        List<CardDto> CommunityCards,
        List<CardDto> HoleCards,
        double WinProbability,
        int Budget,
        int CallAmount
    );

    public record HintResponse(
        string Advice
    );
}
