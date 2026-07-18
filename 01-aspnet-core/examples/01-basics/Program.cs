using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var todos = new List<TodoItem>
{
    new(1, "Read the ASP.NET Core basics guide", false),
    new(2, "Practice explaining middleware order", false)
};

var nextId = todos.Count + 1;

app.MapGet("/", () => Results.Ok(new
{
    Message = "Hello from a modern ASP.NET Core minimal API!",
    Documentation = "/swagger"
}))
.WithName("Hello")
.WithTags("Basics");

var todoGroup = app.MapGroup("/api/todos")
    .WithTags("Todos");

todoGroup.MapGet("/", ([FromQuery] bool? isDone) =>
{
    var query = todos.AsEnumerable();

    if (isDone is not null)
    {
        query = query.Where(todo => todo.IsDone == isDone.Value);
    }

    return Results.Ok(query.OrderBy(todo => todo.Id));
})
.WithName("ListTodos")
.Produces<IReadOnlyCollection<TodoItem>>();

todoGroup.MapGet("/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(item => item.Id == id);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
})
.WithName("GetTodoById")
.Produces<TodoItem>()
.Produces(StatusCodes.Status404NotFound);

todoGroup.MapPost("/", (CreateTodoRequest request) =>
{
    var validationErrors = Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var todo = new TodoItem(nextId++, request.Title.Trim(), false);
    todos.Add(todo);

    return Results.Created($"/api/todos/{todo.Id}", todo);
})
.WithName("CreateTodo")
.Produces<TodoItem>(StatusCodes.Status201Created)
.ProducesValidationProblem();

todoGroup.MapPut("/{id:int}", (int id, UpdateTodoRequest request) =>
{
    var validationErrors = Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var index = todos.FindIndex(item => item.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    var updated = todos[index] with
    {
        Title = request.Title.Trim(),
        IsDone = request.IsDone
    };

    todos[index] = updated;
    return Results.Ok(updated);
})
.WithName("UpdateTodo")
.Produces<TodoItem>()
.ProducesValidationProblem()
.Produces(StatusCodes.Status404NotFound);

todoGroup.MapPatch("/{id:int}/complete", (int id) =>
{
    var index = todos.FindIndex(item => item.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    todos[index] = todos[index] with { IsDone = true };
    return Results.NoContent();
})
.WithName("CompleteTodo")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

todoGroup.MapDelete("/{id:int}", (int id) =>
{
    var removed = todos.RemoveAll(item => item.Id == id);
    return removed == 0 ? Results.NotFound() : Results.NoContent();
})
.WithName("DeleteTodo")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapControllers();

app.Run();

static Dictionary<string, string[]> Validate<TRequest>(TRequest request)
{
    var context = new ValidationContext(request!);
    var results = new List<ValidationResult>();

    if (Validator.TryValidateObject(request!, context, results, validateAllProperties: true))
    {
        return [];
    }

    return results
        .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty),
            (result, memberName) => new { memberName, result.ErrorMessage })
        .GroupBy(error => error.memberName)
        .ToDictionary(
            group => group.Key,
            group => group
                .Select(error => error.ErrorMessage ?? "Invalid value.")
                .ToArray());
}

public sealed record TodoItem(int Id, string Title, bool IsDone);

public sealed record CreateTodoRequest(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string Title);

public sealed record UpdateTodoRequest(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string Title,
    bool IsDone);

