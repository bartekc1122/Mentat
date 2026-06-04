using DotNetEnv;
using OpenAI;
using OpenAI.Chat;

namespace Mentat.Infrastructure.LLM.Client
{
    public class OpenAIChatConnection : ILLMChatConnection
    {
        private const string model = "gpt-5-nano";
        private ChatClient _chat;

        public OpenAIChatConnection()
        {
            string dir = Directory.GetCurrentDirectory();
            string env = Path.Combine(dir, ".env");
            DotNetEnv.Env.Load(env);
            OpenAIClient client = new(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            _chat = client.GetChatClient(model);
        }


    }
}
