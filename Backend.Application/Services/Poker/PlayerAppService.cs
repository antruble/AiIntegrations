using Backend.Application.Interfaces.Poker;
using Backend.Domain.Entities;
using Backend.Domain.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Services.Poker
{
    public class PlayerAppService : IPlayerService
    {
        private readonly IUnitOfWork _unitOfWork;
        public PlayerAppService(
            IUnitOfWork unitOfWork
            )
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> GetUserIdAsync(Guid gameId)
        {
            var players = await _unitOfWork.Players.GetAllAsync(filter: p => !p.IsBot);
            return players.FirstOrDefault()!.Id;
        }
        public async Task<Player> GetPlayerByIdAsync(Guid playerId)
        {
            return await _unitOfWork.Players.GetByIdAsync(playerId) ?? throw new KeyNotFoundException("Nem létezik ez a player");
        }
        public async Task<IList<Player>> GetPlayersAsync(int numOfBots, string playerName = "Player")
        {
            var players = new List<Player>();

            // Player
            var playerSeat = 2;
            players.Add(
                await _unitOfWork.Players.FindPlayerByName(playerName)
                                ?? new Player(Guid.NewGuid(), playerName, 2000, false, playerSeat)
            );

            //Bots
            for (int i = 0; i < numOfBots; i++)
            {
                if (i == playerSeat)
                {
                    numOfBots++; // Ha a player helyére akarnánk botot rakni, akkor kihagyunk egyet a ciklusban, és növelünk a numOfBots-on, hogy a kihagyott botot is létrehozza
                    continue;
                }
                var botName = $"Bot{i}";
                players.Add(
                    await _unitOfWork.Players.FindPlayerByName(botName)
                                    ?? new Player(Guid.NewGuid(), botName, 2000, true, i)
                );
            }


            return [.. players.OrderBy(p => p.Seat)];
        }

    }
}
