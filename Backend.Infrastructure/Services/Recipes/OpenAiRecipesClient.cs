using Backend.Shared.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Infrastructure.Services.Recipes
{
    public class OpenAiRecipesClient
    : BaseOpenAiClient<RecipesRequest, RecipesResponse>
    {
        private const string BaseSystemMessage = @"
            You are an AI chef specialized in generating creative and detailed recipes.
            Based on the user's request, provide 10 recipe suggestions along with their ingredients and preparation steps.
            **Output must be strictly valid JSON only.** 
            Produce **only** a JSON array of objects—no explanatory text, no Markdown—where each object has exactly these three properties:
              ""Title"" (string),
              ""ShortDescription"" (string),
              ""DetailedRecipe"" (string).
            Use **double quotes** for all property names and string values.
            Ensure the entire response is written in Hungarian.
            Incorporate the user's provided ingredients into the recipes when applicable.
            ";
        public OpenAiRecipesClient(IConfiguration cfg)
            : base(cfg,
                BaseSystemMessage)
        { }

        protected override string BuildSystemMessage(RecipesRequest req)
            => _baseSystemMessage;

        protected override string GetUserPrompt(RecipesRequest req)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Felhasználói recept kérés:");
            sb.AppendLine("Leírás: " + req.Description);
            if (req.Ingredients != null && req.Ingredients.Count > 0)
            {
                sb.Append("Elérhető hozzávalók: ");
                sb.AppendLine(string.Join(", ", req.Ingredients));
            }
            return sb.ToString();
        }
        protected override RecipesResponse ParseResponse(string json)
            => new RecipesResponse
            {
                Recipes = JsonSerializer.Deserialize<List<RecipeDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
            };
    }
}
