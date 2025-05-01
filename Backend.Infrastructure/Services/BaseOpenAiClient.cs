using Backend.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Infrastructure.Services
{
    public abstract class BaseOpenAiClient<TRequest, TResponse>
    : IOpenAiClient<TRequest, TResponse>
    {
        protected readonly ChatClient _client;
        protected readonly string _baseSystemMessage;

        protected BaseOpenAiClient(IConfiguration cfg, string baseSystemMessage)
        {
            _client = new ChatClient(
                model: cfg["OpenAi:Model"],
                apiKey: cfg["OpenAi:ApiKey"]
            );
            _baseSystemMessage = baseSystemMessage;
        }

        public async Task<TResponse> ExecuteAsync(TRequest request)
        {
            var systemMessage = BuildSystemMessage(request);
            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemMessage),
                new UserChatMessage(GetUserPrompt(request))
            };

            var result = await _client.CompleteChatAsync(messages, new ChatCompletionOptions { Temperature = 0.5f });
            return ParseResponse(result.Value.Content[0].Text);
        }

        protected abstract string BuildSystemMessage(TRequest request);

        protected abstract string GetUserPrompt(TRequest request);

        protected abstract TResponse ParseResponse(string json);
    }

}
