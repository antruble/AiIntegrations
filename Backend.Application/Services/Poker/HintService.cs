using Backend.Application.Interfaces.Poker;
using Backend.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Backend.Shared.Models;

namespace Backend.Application.Services.Poker
{
    public class HintAppService : IHintService
    {
        private readonly IOpenAiClient<HintRequest, HintResponse> _openAiClient;
        public HintAppService(IOpenAiClient<HintRequest, HintResponse> client)
            => _openAiClient = client;

        public Task<HintResponse> GetHintAsync(HintRequest req)
            => _openAiClient.ExecuteAsync(req);
    }

}
