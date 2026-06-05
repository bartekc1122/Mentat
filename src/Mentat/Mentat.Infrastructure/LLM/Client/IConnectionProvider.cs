using System;
using System.Collections.Generic;
using System.Text;

namespace Mentat.Infrastructure.LLM.Client
{
    public interface IConnectionProvider
    {
        ILLMChatConnection CreateConnection(string schemaName, BinaryData jsonSchema, string systemMessage);
    }
}
