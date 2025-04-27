using Backend.Shared.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Infrastructure.Services.DocumentSummary
{
    public class OpenAiSummaryClient
    : BaseOpenAiClient<DocumentSummaryRequest, DocumentSummaryResponse>
    {
        private const string BaseSystemMessage = 
            "You are an AI assistant specialized in summarizing documents. " +
               "Your task is to first provide a concise, 1-2 sentence summary outlining the main topic and thematic focus of the provided document, " +
               "and then provide a detailed summary. " +
               "Format your answer as a JSON object with two properties: 'ShortSummary' and 'DetailedSummary'. " +
               "Ensure that your entire response is written in Hungarian, regardless of the language or content of the input. " +
               "The summaries must be clear, logically structured, and accurate.";
        public OpenAiSummaryClient(IConfiguration cfg)
            : base(cfg, BaseSystemMessage)
        { }

        protected override string BuildSystemMessage(DocumentSummaryRequest req)
            => _baseSystemMessage + GetStyleDescription(req.Style);

        protected override string GetUserPrompt(DocumentSummaryRequest req)
            => req.Text;

        protected override DocumentSummaryResponse ParseResponse(string json)
            => JsonSerializer.Deserialize<DocumentSummaryResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        private string GetStyleDescription(string style) => style.ToLower() switch
        {
            "academic" => " Please use a scientific and academic tone. ",
            "practical" => " Please use a practical, action-oriented tone. ",
            "simple" => " Please use simple and clear language. ",
            _ => string.Empty
        };
    }

}
