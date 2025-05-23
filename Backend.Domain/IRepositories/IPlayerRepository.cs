﻿using Backend.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Domain.IRepositories
{
    public interface IPlayerRepository : IGenericsRepository<Player>
    {
        Task<Player?> FindPlayerByName(string name);
    }
}
