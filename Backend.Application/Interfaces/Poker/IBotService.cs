using Backend.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces.Poker
{
    public interface IBotService
    {
        Task<PlayerAction> GenerateBotActionAsync(Guid botId, int amount);
    }
}
