namespace Mentat.Infrastructure.LLM
{
    // Central registry of LLM model identifiers used across the project.
    // Change the value here to swap the chat model everywhere.
    public static class LlmModels
    {
        // Chat model for structured-output tasks (speaker resolution, note extraction).
        // OpenAI GPT-5.4 (released 2026-03-05): Chat Completions + strict structured outputs.
        public const string Chat = "gpt-5.4";
    }
}
