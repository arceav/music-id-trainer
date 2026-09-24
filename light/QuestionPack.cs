using System.IO;
using System.Text.Json;

namespace MusicIdTrainer;

internal sealed class QuestionPack
{
    public required string Format { get; init; }
    public required int Version { get; init; }
    public required string Title { get; init; }
    public required List<QuestionCard> Cards { get; init; }

    public static QuestionPack Load(string filePath)
    {
        var pack = JsonSerializer.Deserialize<QuestionPack>(File.ReadAllText(filePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (pack is null || pack.Format != "music-id-trainer" || pack.Version != 1 ||
            string.IsNullOrWhiteSpace(pack.Title) || pack.Cards is not { Count: > 0 })
            throw new InvalidDataException("Expected a Music ID Trainer question pack (version 1) with cards.");

        for (var i = 0; i < pack.Cards.Count; i++)
        {
            var card = pack.Cards[i];
            if (card is null || string.IsNullOrWhiteSpace(card.File) ||
                !double.IsFinite(card.Start) || card.Start < 0 ||
                string.IsNullOrWhiteSpace(card.Question) ||
                card.Choices is not { Length: 5 } ||
                card.Choices.Any(string.IsNullOrWhiteSpace) ||
                card.Answer is < 0 or > 4 || card.Explanation is null)
                throw new InvalidDataException($"Question {i + 1} is incomplete or invalid.");
        }
        return pack;
    }
}

internal sealed class QuestionCard
{
    public required string File { get; init; }
    public required double Start { get; init; }
    public required string Question { get; init; }
    public required string[] Choices { get; init; }
    public required int Answer { get; init; }
    public required string Explanation { get; init; }
}
