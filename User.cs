namespace MusicGame
{
    public class User
    {
        uint ID { get; init; }
        string Name { get; set; }
        string PasswordHash { get; set; }
        string SessionID { get; set; }

    }
}
