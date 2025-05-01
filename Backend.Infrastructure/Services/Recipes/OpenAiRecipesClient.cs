using Backend.Shared.Models;
using Backend.Shared.Models.Recipes;
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
    : BaseOpenAiClient<RecipeRequestDto, RecipesResponseDto>
    {
        private const string BaseSystemMessage = @"
            You are an AI chef specialized in generating creative and detailed recipes.
            Based on the user's request, provide 10 recipe suggestions along with their ingredients and preparation steps.
            Output must be strictly valid JSON only.  
            Produce only a JSON array of objects—no explanatory text—where each object has exactly these properties:
              ""Title"": string,  
              ""ShortDescription"": string,  
              ""DetailedRecipe"": {  
                  ""CookTime"": string,  
                  ""Ingredients"": [string],  
                  ""Steps"": [string]  
              }.
            Use double quotes for all property names and values. Ensure the entire response is in Hungarian.
            Incorporate the user's provided ingredients when applicable.
            ";

        public OpenAiRecipesClient(IConfiguration cfg)
            : base(cfg,
                BaseSystemMessage)
        { }

        protected override string BuildSystemMessage(RecipeRequestDto req)
            => _baseSystemMessage;

        protected override string GetUserPrompt(RecipeRequestDto req)
        {
            var sb = new StringBuilder();
            sb.AppendLine("User's recipe request:");
            sb.AppendLine("Description: " + req.Description);
            if (req.Ingredients != null && req.Ingredients.Count > 0)
            {
                sb.Append("Avaliable ingredients: ");
                sb.AppendLine(string.Join(", ", req.Ingredients));
            }
            return sb.ToString();
        }
        protected override RecipesResponseDto ParseResponse(string json)
        {
            var list = JsonSerializer.Deserialize<List<RecipeSuggestionDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("A deszerializáció során null érték keletkezett.");

            return new RecipesResponseDto(list);
        }

    }
}
