using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared.Models.Recipes
{
    /// <summary>
    /// A backend felé elküldendő recept kérés adatai.
    /// </summary>
    public record RecipeRequestDto(
        string Description,
        List<string> Ingredients
    );

    /// <summary>
    /// A recept javaslatokat tartalmazó DTO.
    /// </summary>
    public record RecipeSuggestionDto(
        string Title,
        string ShortDescription,
        string DetailedRecipe
    );

    public record RecipesResponseDto(IReadOnlyList<RecipeSuggestionDto> Recipes);
}
