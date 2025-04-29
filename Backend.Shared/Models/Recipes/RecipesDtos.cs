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
        DetailedRecipeDto DetailedRecipe
    );

    public record DetailedRecipeDto(
        string CookTime,
        List<string> Ingredients,
        List<string> Steps
    );
    public record RecipesResponseDto(IReadOnlyList<RecipeSuggestionDto> Recipes);
}
