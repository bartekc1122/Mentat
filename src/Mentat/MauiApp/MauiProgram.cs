using Microsoft.Extensions.Logging;
using Mentat.Infrastructure.Embeddings;
using Mentat.Infrastructure.LLM;
using Mentat.Infrastructure.Pipeline;
using Mentat.Infrastructure.Search;
using Mentat.Infrastructure.Storage;
using Mentat.Infrastructure.LLM;
using Mentat.Infrastructure.LLM.Client;
using Mentat.Infrastructure.Transcription;
using Mentat.Services;
using Plugin.Maui.Audio;

namespace Mentat;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton(AudioManager.Current);

		var apiKey = LoadApiKey();
		builder.Services.AddSingleton<IRecordingService, RecordingService>();
		builder.Services.AddSingleton<ITranscriptionService>(_ => new TranscriptionService(apiKey));
		builder.Services.AddSingleton<IConnectionProvider>(_ => new OpenAIChatConnectionProvider(apiKey));
		builder.Services.AddSingleton<SpeakerResolver>();

		// Pipeline przetwarzania spotkania: zapis w SQLite + embeddingi + wyszukiwanie semantyczne.
		string dbPath = Path.Combine(FileSystem.AppDataDirectory, "mentat.db3");
		builder.Services.AddSingleton(_ => new MeetingDatabase(dbPath));
		builder.Services.AddSingleton<IEmbeddingService>(_ => new EmbeddingService(apiKey));
		builder.Services.AddSingleton(_ => new NoteExtractor(apiKey));
		builder.Services.AddSingleton<MeetingProcessor>();
		builder.Services.AddSingleton<SemanticSearchService>();

		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private const string ApiKeyName = "OPENAI_API_KEY";

	private static string LoadApiKey()
	{
		try
		{
			// openai.env is bundled into the app package via Resources/Raw, so read it from there.
			using Stream stream = Task.Run(() => FileSystem.OpenAppPackageFileAsync("openai.env")).GetAwaiter().GetResult();
			using var reader = new StreamReader(stream);
			string content = reader.ReadToEnd();

			foreach (string line in content.Split('\n'))
			{
				string trimmed = line.Trim();
				if (trimmed.StartsWith($"{ApiKeyName}="))
					return trimmed[(ApiKeyName.Length + 1)..].Trim().Trim('"');
			}
		}
		catch
		{
			// .env not bundled (e.g. desktop dev) — fall back to environment variable.
		}

		return Environment.GetEnvironmentVariable(ApiKeyName) ?? "";
	}
}
