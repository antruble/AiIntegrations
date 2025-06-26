using Backend.Shared.Models;
using Backend.Shared.Models.Poker;
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

namespace Backend.Infrastructure.Services.Poker
{
    public class OpenAiHintClient
        : BaseOpenAiClient<HintRequest, HintResponse>
    {
        private const string BaseSystemMessage = @"
You are a world-class Texas Hold’em poker advisor. You will receive a JSON object with these properties: • CommunityCards: list of the shared cards on the table; • HoleCards: list of the player’s two private cards; • WinProbability: estimated chance to win as a decimal between 0.0 and 100.0; • Budget: the player’s remaining chips; • CallAmount: the chips required to call this round  

Analyze the situation by considering in order: 1. Your current made hand strength and any drawing possibilities (flush draw, straight draw, set, etc.); 2. Pot odds: compare CallAmount to Budget.; 3. How WinProbability relates to your hand strength and the community cards.; 4. Pot odds (Call/Budget); only fold if Call/Budget > 20.0 (20 %);

Then choose exactly one of the three actions: • Fold; • Call; • Raise  

If you choose Raise, include a suggested raise amount (an integer).
Respond with a single valid JSON object, with only one property:
{ ""Advice"": ""<Action> - <short Hungarian explanation>"" }

Few-shot examples:
User: {""Community"":[""6♣"",""K♠"",""6♦""],""Hole"":[""K♦"",""7♣""],""WinProb"":28.19,""Budget"":1443,""Call"":0}
Assistant: { ""Advice"": ""Call - van két párunk, de érdemes még várni a többi közös lapra"" }

User: {""Community"":[""A♠"",""K♣"",""Q♦""],""Hole"":[""J♥"",""10♥""],""WinProb"":35,""Budget"":1000,""Call"":100}
Assistant: { ""Advice"": ""Raise 200 - erős sorunk van, ilyen esetben érdemes nyomást gyakorolni"" }

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
            static string SuitSymbol(int suit) => suit switch
            {
                0 => "♣",
                1 => "♦",
                2 => "♥",
                3 => "♠",
                _ => ""
            };

            var community = req.CommunityCards
                .Select(c => $"{c.DisplayValue}{GetSuitSymbol(c.Suit)}")
                .ToArray();
            var hole = req.HoleCards
                .Select(c => $"{c.DisplayValue}{GetSuitSymbol(c.Suit)}")
                .ToArray();

            var promptObj = new
            {
                Community = community,
                Hole = hole,
                WinProb = req.WinProbability,
                Budget = req.Budget,
                Call = req.CallAmount
            };

            return JsonSerializer.Serialize(promptObj);
        }


        protected override HintResponse ParseResponse(string json)
        {
            var result = JsonSerializer.Deserialize<HintResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? throw new JsonException("Failed to deserialize HintResponse from OpenAI response.");
        }

        private string GetSuitSymbol(SuitDto suit) =>
        suit switch
        {
            SuitDto.Clubs => "♣",
            SuitDto.Diamonds => "♦",
            SuitDto.Hearts => "♥",
            SuitDto.Spades => "♠",
            _ => ""
        };
    }
}


