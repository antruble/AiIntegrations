using Backend.Domain.Entities;
using Backend.Domain.IRepositories;
using Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Infrastructure.Repositories
{
    public class GameRepository(ApplicationDbContext context) : GenericsRepository<Game>(context), IGameRepository
    {
        public override async Task<Game?> GetByIdAsync(object id)
        {
            // Feltételezzük, hogy a Game.Id típusa Guid.
            Guid gameId = (Guid)id;
            return await base._dbSet
                .Include(g => g.Players)
                .Include(g => g.CurrentHand)
                    // Ha az aktuális kézhez tartoznak például a CommunityCards:
                    .ThenInclude(hand => hand.CommunityCards)
                .FirstOrDefaultAsync(g => g.Id == gameId);
        }
    }
}
