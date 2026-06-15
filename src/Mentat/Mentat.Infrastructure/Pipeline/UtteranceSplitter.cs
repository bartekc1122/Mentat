using System.Text.RegularExpressions;

namespace Mentat.Infrastructure.Pipeline;

/// <summary>
/// Rozkłada gotowy tekst z rolami (linie <c>Mówca: tekst</c>) na listę wypowiedzi.
/// To NIE jest diaryzacja — zakładamy, że role są już w tekście (powstają na innym branchu).
/// Jeśli ten branch zacznie dostarczać gotowe wypowiedzi, podmieniamy tu źródło bez zmian w pipeline.
/// </summary>
public static class UtteranceSplitter
{
    // "Mówca: treść" — etykieta mówcy do ~40 znaków, bez znaku ':' w środku.
    private static readonly Regex SpeakerLine = new(
        @"^\s*(?<speaker>[^:\r\n]{1,40}):\s*(?<text>.+)$",
        RegexOptions.Compiled);

    public static IReadOnlyList<Utterance> Split(string transcript)
    {
        var result = new List<Utterance>();
        if (string.IsNullOrWhiteSpace(transcript))
            return result;

        foreach (var rawLine in transcript.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            Match m = SpeakerLine.Match(line);
            if (m.Success)
            {
                string speaker = m.Groups["speaker"].Value.Trim();
                string text = m.Groups["text"].Value.Trim();
                result.Add(new Utterance($"u{result.Count + 1}", speaker, text));
            }
            else if (result.Count > 0)
            {
                // Linia bez prefiksu = kontynuacja poprzedniej wypowiedzi.
                Utterance prev = result[^1];
                result[^1] = prev with { Text = $"{prev.Text} {line}".Trim() };
            }
            else
            {
                // Tekst bez ról na samym początku — traktujemy jako jednego, nieznanego mówcę.
                result.Add(new Utterance($"u{result.Count + 1}", "Osoba 1", line));
            }
        }

        return result;
    }
}
