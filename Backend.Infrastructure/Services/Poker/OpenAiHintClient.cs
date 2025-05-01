using Backend.Shared.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Spire.Doc.Fields.Shapes.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Backend.Infrastructure.Services.Poker
{
    public class OpenAiHintClient
        : BaseOpenAiClient<HintRequest, HintResponse>
    {
        private const string BaseSystemMessage = @"
            You are a world-class Texas Hold’em poker advisor. You will receive a JSON object with these properties:
              • CommunityCards: list of the shared cards on the table  
              • HoleCards: list of the player’s two private cards  
              • WinProbability: estimated chance to win as a decimal between 0.0 and 1.0  
              • Budget: the player’s remaining chips  
              • CallAmount: the chips required to call this round  

            Analyze the situation by considering in order:
              1. Your current made hand strength and any drawing possibilities (flush draw, straight draw, set, etc.).  
              2. Pot odds: compare CallAmount to Budget.  
              3. How WinProbability relates to your hand strength and the community cards.  

            Then choose exactly one of the three actions:  
              • Fold  
              • Call  
              • Raise  

            If you choose Raise, include a suggested raise amount (an integer).  

            Respond with a single valid JSON object, with only one property:
              {
                ""Advice"": ""<Action> - <short Hungarian explanation>""
              }

            Examples of valid Advice values:
              • ""Fold - kevés az esély a nyerésre, és magas a pot értéke""  
              • ""Call - jó a pot értéke és kedvezőek az oddsok""  
              • ""Raise 150 - erős a hand és nyomást kell gyakorolni""  

            Do **not** include any other text, markdown or formatting—only the JSON object exactly as specified.
            ";

        public OpenAiHintClient(IConfiguration cfg)
            : base(cfg, BaseSystemMessage)
        {
        }

        protected override string BuildSystemMessage(HintRequest req)
            => _baseSystemMessage;

        protected override string GetUserPrompt(HintRequest req)
        {
            // Include call amount to help AI evaluate fold vs call properly
            return JsonSerializer.Serialize(new
            {
                CommunityCards = req.CommunityCards,
                HoleCards = req.HoleCards,
                WinProbability = req.WinProbability,
                Budget = req.Budget,
                CallAmount = req.CallAmount
            });
        }

        protected override HintResponse ParseResponse(string json)
        {
            var result = JsonSerializer.Deserialize<HintResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? throw new JsonException("Failed to deserialize HintResponse from OpenAI response.");
        }
    }
}


