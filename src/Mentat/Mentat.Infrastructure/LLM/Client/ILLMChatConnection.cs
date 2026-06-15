using System;
using System.Collections.Generic;
using System.Text;

namespace Mentat.Infrastructure.LLM.Client
{
    public interface ILLMChatConnection
    {
        Task<string> MakeCallAsync(string text, CancellationToken cancellationToken = default);
    }
}
