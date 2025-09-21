using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);


var app = builder.Build();


//todos
var todos = new List<Todo>();

app.MapGet("/todos", () => Results.Ok(todos));

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id) =>
{
    var targetToDo = todos.SingleOrDefault(t => id == t.Id);

    return targetToDo is null ? TypedResults.NotFound()
    : TypedResults.Ok(targetToDo);

});

app.MapPost("/todos", (Todo task)=>
{
    todos.Add(task);
    return TypedResults.Created($"/todos/{task.Id}", task);

});


app.MapDelete("/todos/{id}", (int id)=>
{
    todos.RemoveAll(t => id == t.Id);
    return TypedResults.NoContent();
});


app.Run();



public record Todo(int Id ,string Name , DateTime DueDate,bool IsCompleted);
