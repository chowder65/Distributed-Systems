namespace RetroGameExchange.Models;

public enum GameCondition
{
    Mint,
    Good,
    Fair,
    Poor
}

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public int YearPublished { get; set; }
    public string GamingSystem { get; set; } = string.Empty;
    public GameCondition Condition { get; set; }
    public int? PreviousOwners { get; set; }
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
}
