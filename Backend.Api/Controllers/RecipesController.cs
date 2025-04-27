using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Backend.Application.Interfaces;
using Backend.Shared.Models;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IOpenAiClient<RecipesRequest, RecipesResponse> _openAiRecipesClient;

        public RecipesController(
            IOpenAiClient<RecipesRequest, RecipesResponse> openAiRecipesClient)
        {
            _openAiRecipesClient = openAiRecipesClient;
        }
        /// <summary>
        /// Recepteket generáló végpont.
        /// A kérés tartalmazza a recept leírást és a rendelkezésre álló hozzávalókat.
        /// Az AI számára előkészíti a promptot, majd visszaadja a generált recept ajánlásokat.
        /// </summary>
        /// <param name="request">A felhasználó által megadott paraméterek (leírás, hozzávalók)</param>
        /// <returns>A generált receptek listája</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRecipes([FromBody] RecipesRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest("A recept leírása kötelező.");
            }

            // Hívjuk az új kliens metódusát, amely elküldi a promptot az OpenAI API-nak.
            var aiResponse = await _openAiRecipesClient.ExecuteAsync(request)
                ?? throw new Exception();

            return Ok(aiResponse);
        }
    }
}
