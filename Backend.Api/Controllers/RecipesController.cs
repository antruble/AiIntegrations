﻿using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Backend.Application.Interfaces;
using Backend.Shared.Models;
using Backend.Shared.Models.Recipes;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IOpenAiClient<RecipeRequestDto, RecipesResponseDto> _openAiRecipesClient;

        public RecipesController(
            IOpenAiClient<RecipeRequestDto, RecipesResponseDto> openAiRecipesClient)
        {
            _openAiRecipesClient = openAiRecipesClient;
        }
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRecipes([FromBody] RecipeRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest("A recept leírása kötelező.");
            }

            var aiResponse = await _openAiRecipesClient.ExecuteAsync(request)
                ?? throw new Exception();

            return Ok(aiResponse);
        }
    }
}
