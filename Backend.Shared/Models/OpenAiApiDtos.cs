using Backend.Shared.Models.Poker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared.Models
{
    public class DocumentSummaryRequest
    {
        public string Text { get; init; }
        public string Style { get; init; }
    }

    public class DocumentSummaryResponse
    {
        public string ShortSummary { get; set; }
        public string DetailedSummary { get; set; }
    }

    public class RecipesRequest
    {
        /// <summary>
        /// A felhasználó által megadott recept leírása.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A felhasználó által rendelkezésre állónak jelölt hozzávalók listája.
        /// </summary>
        public List<string> Ingredients { get; set; }
    }

    public class RecipesResponse
    {
        public IReadOnlyList<RecipeDto> Recipes { get; set; }
    }

    public class RecipeDto
    {
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public string DetailedRecipe { get; set; }
    }

    public record HintRequest(
        Guid GameId,
        Guid PlayerId,
        List<CardDto> CommunityCards,
        List<CardDto> HoleCards,
        double WinProbability,
        int Budget,
        int CallAmount
    );

    public record HintResponse(
        string Advice
    );
}
