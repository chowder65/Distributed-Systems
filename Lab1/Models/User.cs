namespace RetroGameExchange.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public List<Game> Games { get; set; } = [];
}
