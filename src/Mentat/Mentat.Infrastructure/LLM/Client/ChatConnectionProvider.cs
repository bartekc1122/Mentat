using System;
using System.Collections.Generic;
using System.Text;

namespace Mentat.Infrastructure.LLM.Client
{
    public class OpenAIChatConnectionProvider : IConnectionProvider
    {
        public ILLMChatConnection CreateConnection(string schemaName, BinaryData jsonSchema, string systemMessage)
        {
            return OpenAIChatConnection.Create(schemaName, jsonSchema, systemMessage);
        }
    }
}
