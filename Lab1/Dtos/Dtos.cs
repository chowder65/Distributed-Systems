using RetroGameExchange.Models;

namespace RetroGameExchange.Dtos;

public class Link
{
    public string Rel { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
}

public record ErrorResponse(int Status, string Error, string Message);

public record RegisterRequest(string Name, string Email, string Password, string StreetAddress);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token);

public record UserUpdateRequest(string Name, string StreetAddress);

public record UserResponse(int Id, string Name, string Email, string StreetAddress, List<Link> Links);

public record GameRequest(
    string Name,
    string Publisher,
    int YearPublished,
    string GamingSystem,
    GameCondition Condition,
    int? PreviousOwners);

public record GameResponse(
    int Id,
    string Name,
    string Publisher,
    int YearPublished,
    string GamingSystem,
    string Condition,
    int? PreviousOwners,
    int OwnerId,
    List<Link> Links);

public record GameListResponse(List<GameResponse> Games, List<Link> Links);
