namespace Mentat.Infrastructure.Pipeline;

/// <summary>
/// Dzieli wypowiedzi na nakładające się okna (sliding window). Nakładanie (overlap)
/// sprawia, że temat ucięty na granicy okna trafia do dwóch sąsiednich okien i nie ginie —
/// duplikaty scala później drugi przebieg LLM (<see cref="Consolidator"/>).
/// </summary>
public static class Windowing
{
    public const int DefaultSize = 25;
    public const int DefaultOverlap = 10;

    public static IReadOnlyList<IReadOnlyList<Utterance>> Window(
        IReadOnlyList<Utterance> utterances,
        int size = DefaultSize,
        int overlap = DefaultOverlap)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Rozmiar okna musi być dodatni.");
        if (overlap < 0 || overlap >= size)
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap musi być w zakresie [0, size).");

        var windows = new List<IReadOnlyList<Utterance>>();
        if (utterances.Count == 0)
            return windows;

        int step = size - overlap;
        for (int start = 0; start < utterances.Count; start += step)
        {
            int count = Math.Min(size, utterances.Count - start);
            windows.Add(((List<Utterance>)[.. utterances.Skip(start).Take(count)]));

            // Ostatnie okno objęło już resztę wypowiedzi.
            if (start + count >= utterances.Count)
                break;
        }

        return windows;
    }
}
