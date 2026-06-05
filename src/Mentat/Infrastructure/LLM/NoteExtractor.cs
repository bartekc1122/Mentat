using Mentat.Infrastructure.LLM.Client;

namespace Mentat.Infrastructure.LLM;

    public class NoteExtractor
    {
        private ILLMChatConnection _client;

        public NoteExtractor(ILLMChatConnection client)
        {
            _client = client;
        }

    }

