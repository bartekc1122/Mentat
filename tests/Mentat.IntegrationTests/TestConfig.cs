namespace Mentat.IntegrationTests;

internal static class TestConfig
{
    private const string ApiKeyName = "OPENAI_API_KEY";

    /// <summary>Wczytuje klucz API z pliku .env (kopiowany do katalogu wyjściowego) lub ze zmiennej środowiskowej.</summary>
    public static string LoadApiKey()
    {
        string envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            foreach (string line in File.ReadAllLines(envPath))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith($"{ApiKeyName}="))
                    return trimmed[(ApiKeyName.Length + 1)..].Trim().Trim('"');
            }
        }

        return Environment.GetEnvironmentVariable(ApiKeyName) ?? "";
    }
}
