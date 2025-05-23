﻿using Backend.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces.Poker
{
    public interface IPlayerService
    {
        Task<IList<Player>> GetPlayersAsync(int numOfBots, string playerName = "Player");
        Task<Guid> GetUserIdAsync(Guid gameId);
        Task<Player> GetPlayerByIdAsync(Guid playerId);

    }
}
