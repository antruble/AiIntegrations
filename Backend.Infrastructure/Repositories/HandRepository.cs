using Backend.Domain.Entities;
using Backend.Domain.IRepositories;
using Backend.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Infrastructure.Repositories
{
    public class HandRepository : GenericsRepository<Hand>, IHandRepository
    {
        public HandRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
