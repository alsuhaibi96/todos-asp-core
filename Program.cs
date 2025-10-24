using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb> (opt=>opt.UseInMemoryDatabase("ToDoList"));
// builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

var app = builder.Build();

app.Use(async (context,next)=>{
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] finshed");
});


if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

// app.MapGet("/users/{userId}/books/{bookId}", 
//     (int userId, int bookId) => $"The user id is {userId} and book id is {bookId}");
app.MapGet("/", () => "hello world");
// app.MapGet("/todoitems",async(TodoDb db)=>
// await db.Todos.ToListAsync());

app.MapGet("/todoitems",(ITaskService service)=>service.GetTodos());

app.MapGet("/todoitems/complete",async(TodoDb db)=>
   await db.Todos.Where(t => t.IsComplete).ToListAsync());

app.MapGet("/todoitems/{id}",async(int id,ITaskService service)=>
 service.GetTodoById(id)
        is Todo todo
        ? Results.Ok(todo)
        :Results.NotFound());

app.MapPost("/todoitems", async (Todo todo, TodoDb db,ITaskService service) =>
{
    service.AddTodo(todo);
    return Results.Created($"/todoitems/{todo.Id}", todo);
}).AddEndpointFilter(async (context, next)=>{
    var taskArgument=context.GetArgument<Todo>(0);
    var errors=new Dictionary<string,string[]>();
    if(taskArgument.DueDate<DateTime.UtcNow){
        errors.Add(nameof(Todo.DueDate),["can not have due date in the past"]);
    }

     if(taskArgument.IsComplete){
        errors.Add(nameof(Todo.IsComplete),["can not add completed todo"]);
    }

     if(errors.Count>0){
      return Results.ValidationProblem(errors);
    }

    return await next(context);
});
        

app.MapPut("/todoitems/{id}",async(int id ,Todo inputToDo, TodoDb db)=>
{
    var todo=await db.Todos.FindAsync(id);

    if (todo is null)
     return Results.NotFound();

     todo.Name=inputToDo.Name;
     todo.IsComplete = inputToDo.IsComplete;

     await db.SaveChangesAsync();

     return Results.NoContent();
});




app.MapDelete("/todoitems/{id}",async(int id , TodoDb db)=>
{

   if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});
        

app.Run();


interface ITaskService {
    Todo? GetTodoById(int id);
    List<Todo> GetTodos();
    void DeleteTodoById(int id);
    Todo AddTodo(Todo task);
}

class InMemoryTaskService: ITaskService {
    private readonly List<Todo> _todos=[];

    public Todo AddTodo(Todo task){
        _todos.Add(task);
        return task;
    }

    public void DeleteTodoById(int id){
        _todos.RemoveAll(task=>id==task.Id);
    }

    public Todo? GetTodoById(int id){
        return _todos.SingleOrDefault(t=>id==t.Id);
    }

    public List<Todo> GetTodos( ){
        return _todos;
    }
}