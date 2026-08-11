var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var ideas = new Dictionary<int, Idea>();
var nextId = 1;

app.MapPost("/idea", (IdeaBody body) =>
{
    var idea = new Idea
    {
        Id = nextId++,
        Title = body.Title ?? string.Empty,
        Description = body.Description ?? string.Empty
    };
    ideas[idea.Id] = idea;
    return Results.Created($"/idea/{idea.Id}", idea);
});

app.MapGet("/idea", () => Results.Ok(ideas.Values));

app.MapGet("/idea/{id:int}", (int id) =>
{
    if (!ideas.TryGetValue(id, out var idea))
        return Results.NotFound();
    return Results.Ok(idea);
});

app.MapPut("/idea/{id:int}", (int id, IdeaBody body) =>
{
    if (!ideas.ContainsKey(id))
        return Results.NotFound();

    var idea = new Idea
    {
        Id = id,
        Title = body.Title ?? string.Empty,
        Description = body.Description ?? string.Empty
    };
    ideas[id] = idea;
    return Results.Ok(idea);
});

app.MapPatch("/idea/{id:int}", (int id, IdeaPatchBody body) =>
{
    if (!ideas.TryGetValue(id, out var idea))
        return Results.NotFound();

    if (body.Title is not null)
        idea.Title = body.Title;
    if (body.Description is not null)
        idea.Description = body.Description;

    return Results.Ok(idea);
});

app.MapDelete("/idea/{id:int}", (int id) =>
{
    if (!ideas.Remove(id))
        return Results.NotFound();
    return Results.NoContent();
});

app.Run();

class Idea
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

class IdeaBody
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

class IdeaPatchBody
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}
