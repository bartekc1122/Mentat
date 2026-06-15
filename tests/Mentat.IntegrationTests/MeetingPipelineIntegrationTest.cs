using Mentat.Infrastructure.Embeddings;
using Mentat.Infrastructure.LLM;
using Mentat.Infrastructure.Pipeline;
using Mentat.Infrastructure.Search;
using Mentat.Infrastructure.Storage;
using Xunit;

namespace Mentat.IntegrationTests;

public class MeetingPipelineIntegrationTest
{
    // Dwa wyraźne tematy; krótkie okna wymuszą nakładanie i scalanie duplikatów.
    private const string Transcript = """
    Anna: Dzień dobry, zacznijmy od problemu z logowaniem.
    Bartek: Logowanie się wykłada, błąd dotyczy tokenu OAuth.
    Anna: Token OAuth wygasa i nie odświeża się poprawnie.
    Bartek: Proponuję przejść na nowy provider OAuth.
    Anna: Dobrze, decydujemy: przechodzimy na nowy provider OAuth.
    Bartek: Naprawię odświeżanie tokenu do piątku.
    Anna: Przejdźmy teraz do raportu sprzedaży za marzec.
    Bartek: Sprzedaż w marcu wzrosła o dziesięć procent.
    Anna: Potrzebujemy podsumowania wyników w formie raportu.
    Bartek: Zgoda, raport przyda się na spotkanie z zarządem.
    Anna: Ustalmy, że przygotujesz raport sprzedaży do środy.
    Bartek: Jasne, przygotuję raport sprzedaży do środy.
    """;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ProcessAsync_StoresItemsWithQuotesAndEmbeddings_AndSearchFindsTopic()
    {
        string apiKey = TestConfig.LoadApiKey();
        Assert.False(string.IsNullOrWhiteSpace(apiKey), "Brak OPENAI_API_KEY (.env lub zmienna środowiskowa).");

        string dbPath = Path.Combine(Path.GetTempPath(), $"mentat_test_{Guid.NewGuid():N}.db3");
        MeetingDatabase? database = null;
        try
        {
            database = new MeetingDatabase(dbPath);
            var extractor = new NoteExtractor(apiKey);
            IEmbeddingService embeddings = new EmbeddingService(apiKey);
            // Małe okna (size=6, overlap=2) → kilka nakładających się okien dla 12 wypowiedzi.
            var processor = new MeetingProcessor(database, extractor, embeddings, windowSize: 6, windowOverlap: 2);

            const int projectId = 1;
            ProcessResult result = await processor.ProcessAsync(Transcript, projectId);

            // Spotkanie zapisane z timestampem, elementy istnieją.
            Assert.True(result.Meeting.Id > 0);
            Assert.True(result.NoteCount > 0);
            Assert.True(result.Meeting.CreatedAt > DateTime.MinValue);

            List<Note> notes = await database.GetNotesByMeetingAsync(result.Meeting.Id);
            Assert.NotEmpty(notes);

            // Każdy element jest informacją albo zadaniem, ma treść, dosłowny cytat i embedding.
            Assert.All(notes, n =>
            {
                Assert.Contains(n.Kind, new[] { ItemKinds.Informacja, ItemKinds.Zadanie });
                Assert.False(string.IsNullOrWhiteSpace(n.Content));
                Assert.False(string.IsNullOrWhiteSpace(n.Quote));
                Assert.True(n.Embedding is { Length: > 0 } && n.EmbeddingDim > 0);
            });

            // W rozmowie są zadania (np. naprawa tokenu, raport) → powinno powstać przynajmniej jedno 'zadanie'.
            Assert.Contains(notes, n => n.Kind == ItemKinds.Zadanie);

            // Wyszukiwanie semantyczne znajduje rozmowę o logowaniu/OAuth.
            var search = new SemanticSearchService(database, embeddings);
            var hits = await search.SearchAsync("problem z logowaniem i tokenem OAuth", projectId, topK: 5);

            Assert.NotEmpty(hits);
            Assert.True(hits[0].Score > 0.2, $"Zbyt niskie dopasowanie: {hits[0].Score}");
            Assert.Contains(hits, h =>
                (h.Content + " " + h.Quote).Contains("OAuth", StringComparison.OrdinalIgnoreCase) ||
                (h.Content + " " + h.Quote).Contains("logowan", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (database is not null)
                await database.CloseAsync();
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
