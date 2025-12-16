using System.Text.Json.Serialization;

namespace MusicGame.Structs;

public class PlayerGuess
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("guess")]
    public List<GuessDetail> GuessDetails { get; set; } = [];
}
