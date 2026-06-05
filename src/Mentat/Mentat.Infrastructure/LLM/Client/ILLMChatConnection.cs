using System;
using System.Collections.Generic;
using System.Text;

namespace Mentat.Infrastructure.LLM.Client
{
    public interface ILLMChatConnection
    {
        ILLMChatConnection Create(string schemaName, BinaryData jsonSchema, string systemMessage);
    }
}
