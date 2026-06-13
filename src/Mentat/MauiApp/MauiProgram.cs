using Microsoft.Extensions.Logging;
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

		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static string LoadApiKey()
	{
		var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
		if (File.Exists(envPath))
			DotNetEnv.Env.Load(envPath);

		return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
	}
}
