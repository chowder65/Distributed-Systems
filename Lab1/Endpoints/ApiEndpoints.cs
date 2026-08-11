using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RetroGameExchange.Data;
using RetroGameExchange.Dtos;
using RetroGameExchange.Models;
using RetroGameExchange.Services;

namespace RetroGameExchange.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapPost("/api/users", Register).AllowAnonymous();
        app.MapPost("/api/auth/login", Login).AllowAnonymous();

        app.MapGet("/api/users/{userId:int}", GetUser).RequireAuthorization();
        app.MapPut("/api/users/{userId:int}", UpdateUser).RequireAuthorization();

        app.MapPost("/api/games", CreateGame).RequireAuthorization();
        app.MapGet("/api/games/{gameId:int}", GetGame).RequireAuthorization();
        app.MapPut("/api/games/{gameId:int}", UpdateGame).RequireAuthorization();
        app.MapDelete("/api/games/{gameId:int}", DeleteGame).RequireAuthorization();
        app.MapGet("/api/games", SearchGames).RequireAuthorization();
    }

    private static string Base(HttpRequest request) =>
        $"{request.Scheme}://{request.Host.Value}";

    private static IResult Error(int status, string error, string message) =>
        Results.Json(new ErrorResponse(status, error, message), statusCode: status);

    private static List<Link> UserLinks(int userId, string baseUrl) =>
    [
        new Link { Rel = "self", Href = $"{baseUrl}/api/users/{userId}", Method = "GET" },
        new Link { Rel = "update", Href = $"{baseUrl}/api/users/{userId}", Method = "PUT" },
        new Link { Rel = "games", Href = $"{baseUrl}/api/games?ownerId={userId}", Method = "GET" }
    ];

    private static List<Link> GameLinks(int gameId, int ownerId, string baseUrl) =>
    [
        new Link { Rel = "self", Href = $"{baseUrl}/api/games/{gameId}", Method = "GET" },
        new Link { Rel = "owner", Href = $"{baseUrl}/api/users/{ownerId}", Method = "GET" },
        new Link { Rel = "update", Href = $"{baseUrl}/api/games/{gameId}", Method = "PUT" },
        new Link { Rel = "delete", Href = $"{baseUrl}/api/games/{gameId}", Method = "DELETE" },
        new Link { Rel = "games", Href = $"{baseUrl}/api/games", Method = "GET" }
    ];

    private static UserResponse ToUser(User user, string baseUrl) =>
        new(user.Id, user.Name, user.Email, user.StreetAddress, UserLinks(user.Id, baseUrl));

    private static GameResponse ToGame(Game game, string baseUrl) =>
        new(
            game.Id,
            game.Name,
            game.Publisher,
            game.YearPublished,
            game.GamingSystem,
            game.Condition.ToString().ToLowerInvariant(),
            game.PreviousOwners,
            game.OwnerId,
            GameLinks(game.Id, game.OwnerId, baseUrl));

    private static async Task<IResult> Register(RegisterRequest body, AppDbContext db, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(body.Name) ||
            string.IsNullOrWhiteSpace(body.Email) ||
            string.IsNullOrWhiteSpace(body.Password) ||
            string.IsNullOrWhiteSpace(body.StreetAddress))
        {
            return Error(400, "Bad Request", "Name, email, password, and street address are required.");
        }

        var email = body.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Error(409, "Conflict", "A user with that email already exists.");

        var user = new User
        {
            Name = body.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
            StreetAddress = body.StreetAddress.Trim()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var baseUrl = Base(request);
        return Results.Created($"{baseUrl}/api/users/{user.Id}", ToUser(user, baseUrl));
    }

    private static async Task<IResult> Login(LoginRequest body, AppDbContext db, TokenService tokens)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
            return Error(400, "Bad Request", "Email and password are required.");

        var email = body.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
            return Error(401, "Unauthorized", "Invalid email or password.");

        var (token, _) = tokens.CreateToken(user);
        return Results.Ok(new LoginResponse(token));
    }

    private static async Task<IResult> GetUser(int userId, AppDbContext db, HttpRequest request)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return Error(404, "Not Found", $"User {userId} was not found.");

        return Results.Ok(ToUser(user, Base(request)));
    }

    private static async Task<IResult> UpdateUser(
        int userId,
        UserUpdateRequest body,
        AppDbContext db,
        ClaimsPrincipal principal)
    {
        if (userId != principal.GetUserId())
            return Error(403, "Forbidden", "You may only update your own user profile.");

        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.StreetAddress))
            return Error(400, "Bad Request", "Name and street address are required.");

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return Error(404, "Not Found", $"User {userId} was not found.");

        user.Name = body.Name.Trim();
        user.StreetAddress = body.StreetAddress.Trim();
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> CreateGame(
        GameRequest body,
        AppDbContext db,
        ClaimsPrincipal principal,
        HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(body.Name) ||
            string.IsNullOrWhiteSpace(body.Publisher) ||
            string.IsNullOrWhiteSpace(body.GamingSystem))
        {
            return Error(400, "Bad Request", "Name, publisher, and gaming system are required.");
        }

        var ownerId = principal.GetUserId();
        var game = new Game
        {
            Name = body.Name.Trim(),
            Publisher = body.Publisher.Trim(),
            YearPublished = body.YearPublished,
            GamingSystem = body.GamingSystem.Trim(),
            Condition = body.Condition,
            PreviousOwners = body.PreviousOwners,
            OwnerId = ownerId
        };

        db.Games.Add(game);
        await db.SaveChangesAsync();

        var baseUrl = Base(request);
        return Results.Created($"{baseUrl}/api/games/{game.Id}", ToGame(game, baseUrl));
    }

    private static async Task<IResult> GetGame(int gameId, AppDbContext db, HttpRequest request)
    {
        var game = await db.Games.FindAsync(gameId);
        if (game is null)
            return Error(404, "Not Found", $"Game {gameId} was not found.");

        return Results.Ok(ToGame(game, Base(request)));
    }

    private static async Task<IResult> UpdateGame(
        int gameId,
        GameRequest body,
        AppDbContext db,
        ClaimsPrincipal principal)
    {
        var game = await db.Games.FindAsync(gameId);
        if (game is null)
            return Error(404, "Not Found", $"Game {gameId} was not found.");

        if (game.OwnerId != principal.GetUserId())
            return Error(403, "Forbidden", "Only the game owner may update this game.");

        if (string.IsNullOrWhiteSpace(body.Name) ||
            string.IsNullOrWhiteSpace(body.Publisher) ||
            string.IsNullOrWhiteSpace(body.GamingSystem))
        {
            return Error(400, "Bad Request", "Name, publisher, and gaming system are required.");
        }

        game.Name = body.Name.Trim();
        game.Publisher = body.Publisher.Trim();
        game.YearPublished = body.YearPublished;
        game.GamingSystem = body.GamingSystem.Trim();
        game.Condition = body.Condition;
        game.PreviousOwners = body.PreviousOwners;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteGame(int gameId, AppDbContext db, ClaimsPrincipal principal)
    {
        var game = await db.Games.FindAsync(gameId);
        if (game is null)
            return Error(404, "Not Found", $"Game {gameId} was not found.");

        if (game.OwnerId != principal.GetUserId())
            return Error(403, "Forbidden", "Only the game owner may delete this game.");

        db.Games.Remove(game);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> SearchGames(
        AppDbContext db,
        HttpRequest request,
        string? name = null,
        int? ownerId = null)
    {
        var query = db.Games.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(g => EF.Functions.Like(g.Name, $"%{name.Trim()}%"));

        if (ownerId is not null)
            query = query.Where(g => g.OwnerId == ownerId);

        var baseUrl = Base(request);
        var games = await query.OrderBy(g => g.Name).ToListAsync();
        var items = games.Select(g => ToGame(g, baseUrl)).ToList();

        var self = $"{baseUrl}/api/games";
        if (!string.IsNullOrWhiteSpace(name) || ownerId is not null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(name))
                parts.Add($"name={Uri.EscapeDataString(name)}");
            if (ownerId is not null)
                parts.Add($"ownerId={ownerId}");
            self = $"{self}?{string.Join('&', parts)}";
        }

        return Results.Ok(new GameListResponse(
            items,
            [new Link { Rel = "self", Href = self, Method = "GET" }]));
    }
}
