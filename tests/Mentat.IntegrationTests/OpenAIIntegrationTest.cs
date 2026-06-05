using Mentat.Infrastructure.LLM.Client;
using System.Text.Json;
using System.Text.Json.Serialization;

using Xunit;

namespace Mentat.IntegrationTests;

public sealed class TextOutput
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public class OpenAIIntegrationTest
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PingApi_Should_Return_TextOutput()
    {
        // Arrange
        var schema = BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "text": {
              "type": "string"
            }
          },
          "required": ["text"],
          "additionalProperties": false
        }
        """);

        var client = OpenAIChatConnection.Create(
            "text_output",
            schema,
            "Return only JSON matching the schema.");

        // Act
        string json = await client.MakeCallAsync("Say: hello from integration test");

        var result = JsonSerializer.Deserialize<TextOutput>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Text));
        Console.WriteLine(result);
    }
}