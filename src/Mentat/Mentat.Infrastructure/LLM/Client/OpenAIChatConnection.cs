
using OpenAI.Chat;
using System.ClientModel;

namespace Mentat.Infrastructure.LLM.Client
{
    public class OpenAIChatConnection : ILLMChatConnection
    {
        private const string model = "gpt-5-nano";
        private SystemChatMessage _systemMessage;


        private ChatClient _client;
        public ChatCompletionOptions _options;

        private OpenAIChatConnection(ChatClient client, ChatCompletionOptions options, SystemChatMessage systemMessage)
        {
            _client = client;
            _options = options;
            _systemMessage = systemMessage;
        }

        public static OpenAIChatConnection Create(string schemaName, BinaryData jsonSchema, string systemMessage)
        {
            string dir = Directory.GetCurrentDirectory();
            string env = Path.Combine(dir, ".env");
            DotNetEnv.Env.Load(env);
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (apiKey == null)
                throw new Exception();

            return Create(schemaName, jsonSchema, systemMessage, apiKey);
        }

        public static OpenAIChatConnection Create(string schemaName, BinaryData jsonSchema, string systemMessage, string apiKey)
        {
            ChatClient client = new(
                    model: model,
                    credential: new ApiKeyCredential(apiKey)
                    );

            return new OpenAIChatConnection(client, GetOptions(schemaName, jsonSchema), new SystemChatMessage(systemMessage));
        }

        public static ChatCompletionOptions GetOptions(string schemaName, BinaryData jsonSchema)
        {

            return
                new ChatCompletionOptions()
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: schemaName,
                    jsonSchema: jsonSchema,
                    jsonSchemaIsStrict: true)
                };
        }

        public async Task<string> MakeCallAsync(string text, CancellationToken cancellationToken = default)
        {
            ChatCompletion completion =  await _client.CompleteChatAsync(
            [
                _systemMessage,

                new UserChatMessage($"""
                Restructure this text into the requested JSON schema:
                  
                {text}
                """)
            ],
            _options,
            cancellationToken);

            var json = completion.Content[0].Text;

            System.Diagnostics.Debug.WriteLine(json);

            return json;
        }
    }
       
}
