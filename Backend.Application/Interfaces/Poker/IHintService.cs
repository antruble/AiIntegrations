using Backend.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces.Poker
{
    public interface IHintService
    {
        Task<HintResponse> GetHintAsync(HintRequest request);
    }
}
