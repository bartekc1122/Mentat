using System;
using System.Collections.Generic;
using System.Text;

namespace Mentat.Infrastructure.LLM.Client
{
    public class OpenAIChatConnectionProvider : IConnectionProvider
    {
        private readonly string? _apiKey;

        public OpenAIChatConnectionProvider()
        {
        }

        public OpenAIChatConnectionProvider(string apiKey)
        {
            _apiKey = apiKey;
        }

        public ILLMChatConnection CreateConnection(string schemaName, BinaryData jsonSchema, string systemMessage, string? model = null)
        {
            return _apiKey is null
                ? OpenAIChatConnection.Create(schemaName, jsonSchema, systemMessage, model)
                : OpenAIChatConnection.Create(schemaName, jsonSchema, systemMessage, _apiKey, model);
        }
    }
}
