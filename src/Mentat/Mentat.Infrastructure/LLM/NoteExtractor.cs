using Mentat.Infrastructure.LLM.Client;

namespace Mentat.Infrastructure.LLM;

    public class NoteExtractor
    {
        private ILLMChatConnection _client;

        public NoteExtractor(IConnectionProvider provider)
        {
            _client = provider.CreateConnection("note_extractor", BinaryData.FromString("{}"), "Extract notes from text.");
        }

    }

