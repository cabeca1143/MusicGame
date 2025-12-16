namespace MusicGame.Structs;

public class GameData
{
    public string Theme { get; set; } = "";
    public bool AcceptingSubmissions { get; set; }
    public bool Guessing { get; set; }
    public List<Submission> Submissions { get; set; } = [];
    public List<PlayerGuess> Guesses { get; set; } = [];
}
