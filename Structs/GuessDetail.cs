using System.Text.Json.Serialization;

namespace MusicGame.Structs;

public class GuessDetail
{
    public int UserId { get; set; }

    [JsonPropertyName("id")]
    public int SubmissionId { get; set; }
}
