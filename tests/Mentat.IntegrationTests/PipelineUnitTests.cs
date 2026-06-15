using Mentat.Infrastructure.Pipeline;
using Xunit;

namespace Mentat.IntegrationTests;

public class PipelineUnitTests
{
    [Fact]
    public void Split_RoleLabeledLines_AssignsSequentialRefs()
    {
        const string transcript = """
        Anna: Musimy poprawić logowanie.
        Bartek: Problem dotyczy tokenu OAuth.
        """;

        var utterances = UtteranceSplitter.Split(transcript);

        Assert.Equal(2, utterances.Count);
        Assert.Equal("u1", utterances[0].Ref);
        Assert.Equal("Anna", utterances[0].Speaker);
        Assert.Equal("Musimy poprawić logowanie.", utterances[0].Text);
        Assert.Equal("u2", utterances[1].Ref);
        Assert.Equal("Bartek", utterances[1].Speaker);
    }

    [Fact]
    public void Split_LineWithoutPrefix_ContinuesPreviousUtterance()
    {
        const string transcript = """
        Anna: Pierwsze zdanie.
        ciąg dalszy bez prefiksu.
        Bartek: Druga osoba.
        """;

        var utterances = UtteranceSplitter.Split(transcript);

        Assert.Equal(2, utterances.Count);
        Assert.Equal("Pierwsze zdanie. ciąg dalszy bez prefiksu.", utterances[0].Text);
    }

    [Fact]
    public void Window_OverlappingWindows_ShareBoundaryUtterances()
    {
        var utterances = Enumerable.Range(1, 50)
            .Select(i => new Utterance($"u{i}", "Osoba", $"tekst {i}"))
            .ToList();

        var windows = Windowing.Window(utterances, size: 25, overlap: 10);

        Assert.Equal(3, windows.Count);
        // Krok = 25 - 10 = 15, więc okno 0 (u1..u25) i okno 1 (u16..u40) współdzielą u16..u25.
        var shared = windows[0].Select(u => u.Ref).Intersect(windows[1].Select(u => u.Ref)).ToList();
        Assert.Equal(10, shared.Count);
        Assert.Contains("u16", shared);
        Assert.Contains("u25", shared);
        // Ostatnie okno obejmuje resztę bez wychodzenia poza zakres.
        Assert.Equal("u50", windows[^1][^1].Ref);
    }

    [Fact]
    public void Window_EmptyInput_ReturnsNoWindows()
    {
        var windows = Windowing.Window([]);
        Assert.Empty(windows);
    }
}
