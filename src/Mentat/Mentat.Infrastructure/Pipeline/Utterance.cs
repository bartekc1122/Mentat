using System.Text.Json.Serialization;

namespace Mentat.Infrastructure.Pipeline;

/// <summary>
/// Pojedyncza wypowiedź z transkryptu (po rozłożeniu tekstu z rolami).
/// To lekki rekord, nie encja bazy danych — listę wypowiedzi serializujemy do
/// <c>Meeting.UtterancesJson</c>, a <see cref="Ref"/> służy jako stabilny link do cytatu.
/// </summary>
public sealed record Utterance(
    [property: JsonPropertyName("ref")] string Ref,
    [property: JsonPropertyName("speaker")] string Speaker,
    [property: JsonPropertyName("text")] string Text);
