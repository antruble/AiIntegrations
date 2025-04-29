using Backend.Application.Interfaces.DocumentSummary;
using Backend.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Backend.Shared.Models;
using Backend.Shared.Models.DocumentSummary;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentSummaryController : Controller
    {
        private readonly IOpenAiClient<DocumentSummaryRequest, DocumentSummaryResponse> _openAiDocSummClient;
        private readonly IDocumentSummaryService _documentSummaryService;

        public DocumentSummaryController(IDocumentSummaryService documentSummaryService, IOpenAiClient<DocumentSummaryRequest, DocumentSummaryResponse> openAiDocSummClient)
        {
            _documentSummaryService = documentSummaryService;
            _openAiDocSummClient = openAiDocSummClient;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string style)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Fájl nem lett feltöltve.");
            }

            // Ellenőrizzük a fájl típusát
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".txt" && extension != ".pdf" && extension != ".doc" && extension != ".docx")
            {
                return BadRequest("Csak txt és pdf és doc fájlok támogatottak.");
            }

            var extractedText = await _documentSummaryService.ExtractTextAsync(file);

            var request = new DocumentSummaryRequest
            {
                Text = extractedText,
                Style = style
            };

            var apiResponse = await _openAiDocSummClient.ExecuteAsync(request);


            return Ok(apiResponse);
        }
    }
}
