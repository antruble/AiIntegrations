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
            You are a world-class poker strategist. You will receive a JSON object with these properties:
              • CommunityCards: the shared cards on the table  
              • HoleCards: the player’s two private cards  
              • WinProbability: the player’s chance to win (0.0–1.0)  
              • Budget: the player’s remaining chips  
              • CallAmount: the chips required to call this round  

            Based on these inputs, think through:
              1. Your current made hand strength and possible draws (flush, straight, set, etc.).  
              2. The pot odds: compare CallAmount to Budget.  
              3. The WinProbability in light of your hand and the community cards.  

            Then produce **exactly** three possible actions with concise reasoning for each:
              1. Fold  
              2. Call  
              3. Raise (include a suggested raise amount)

            Format your response **strictly** as valid JSON with one property 'Advice'
              - ""Advice"": one of 'Fold', 'Call', 'Raise - amount' ---   a short simple Hungarian sentence explaining why

            Do not include any extra text or formatting—only the JSON object.
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


