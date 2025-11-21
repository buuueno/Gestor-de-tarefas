using GestorDeTarefas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<AppDataContext>(options =>
{
    options.UseSqlite("Data Source=GestorTarefas.db");
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
});

var app = builder.Build();


app.UseCors("AllowAll");


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDataContext>();
    db.Database.EnsureCreated();
}

app.UseExceptionHandler("/error");
app.UseHttpsRedirection();



app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapGet("/error", (HttpContext httpContext) =>
{
    var exception = httpContext.Features.Get<IExceptionHandlerPathFeature>();
    return Results.Problem(
        detail: exception?.Error?.StackTrace,
        title: exception?.Error?.Message
    );
});


app.Logger.LogInformation("API Iniciada com CORS liberado!");

app.MapGet("/", () => "API de Tarefas e Categorias conectada ao React!");


app.MapGet("/api/categoria/listar", ([FromServices] AppDataContext banco) =>
{
    if (banco.Categorias.Any())
        return Results.Ok(banco.Categorias.Include(c => c.Tarefas).ToList());
    return Results.NotFound("Nenhuma categoria encontrada.");
});

app.MapPost("/api/categoria/cadastrar", async (Categoria categoria, AppDataContext banco, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Tentando cadastrar nova categoria: {Nome}", categoria.Nome);

        var categoriaExistente = await banco.Categorias.FirstOrDefaultAsync(x => x.Nome == categoria.Nome);
        if (categoriaExistente is not null)
        {
            logger.LogWarning("Categoria já existe: {Nome}", categoria.Nome);
            return Results.Conflict("Categoria já cadastrada");
        }

        banco.Categorias.Add(categoria);
        await banco.SaveChangesAsync();

        logger.LogInformation("Categoria cadastrada com sucesso: {Nome}", categoria.Nome);
        return Results.Created($"/api/categoria/{categoria.Id}", categoria);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao cadastrar categoria: {Nome}", categoria?.Nome);
        return Results.Problem($"Erro ao cadastrar categoria: {ex.Message}");
    }
});

app.MapGet("/api/categoria/{id}", ([FromRoute] int id, [FromServices] AppDataContext banco) =>
{
    var categoria = banco.Categorias.Include(c => c.Tarefas).FirstOrDefault(x => x.Id == id);
    return categoria is null ? Results.NotFound("Categoria não encontrada") : Results.Ok(categoria);
});

app.MapPut("/api/categoria/{id}", ([FromRoute] int id, [FromBody] Categoria categoria, [FromServices] AppDataContext banco) =>
{
    var categoriaExistente = banco.Categorias.Find(id);
    if (categoriaExistente is null)
        return Results.NotFound("Categoria não encontrada");

    categoriaExistente.Nome = categoria.Nome;
    banco.SaveChanges();
    return Results.Ok(categoriaExistente);
});

app.MapDelete("/api/categoria/{id}", ([FromRoute] int id, [FromServices] AppDataContext banco) =>
{
    var categoria = banco.Categorias.Find(id);
    if (categoria is null)
        return Results.NotFound("Categoria não encontrada");

    banco.Categorias.Remove(categoria);
    banco.SaveChanges();
    return Results.Ok(categoria);
});


app.MapGet("/api/tarefa/listar", async ([FromServices] AppDataContext banco) =>
{
    var tarefas = await banco.Tarefas
        .Include(t => t.Categoria)
        .Include(t => t.Usuario)
        .ToListAsync();

    return tarefas.Count == 0 ? Results.NotFound("Nenhuma tarefa encontrada.") : Results.Ok(tarefas);
});
// CADASTRAR TAREFA
app.MapPost("/api/tarefa/cadastrar", async (Tarefa tarefa, AppDataContext banco, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Tentando cadastrar nova tarefa: {Titulo}", tarefa.Titulo);

        if (string.IsNullOrWhiteSpace(tarefa.Titulo))
            return Results.BadRequest("O título é obrigatório");

        var categoria = await banco.Categorias.FindAsync(tarefa.CategoriaId);
        if (categoria is null)
        {
            logger.LogWarning("Categoria não encontrada: {CategoriaId}", tarefa.CategoriaId);
            return Results.NotFound("Categoria não encontrada");
        }

        var usuario = await banco.Usuarios.FindAsync(tarefa.UsuarioId);
        if (usuario is null)
        {
            logger.LogWarning("Usuário não encontrado: {UsuarioId}", tarefa.UsuarioId);
            return Results.NotFound("Usuário não encontrado");
        }

        // Ajustar campos padrão caso não enviados
        if (tarefa.CriadoEm == default)
            tarefa.CriadoEm = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(tarefa.Status))
            tarefa.Status = "Pendente";

        banco.Tarefas.Add(tarefa);
        await banco.SaveChangesAsync();

        logger.LogInformation("Tarefa cadastrada com sucesso: {Id}", tarefa.Id);
        return Results.Created($"/api/tarefa/{tarefa.Id}", tarefa);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao cadastrar tarefa: {Titulo}", tarefa?.Titulo);
        return Results.Problem($"Erro ao cadastrar tarefa: {ex.Message}");
    }
});
app.MapGet("/api/usuario/listar", async (AppDataContext banco) =>
{
    var usuarios = await banco.Usuarios.ToListAsync();
    return Results.Ok(usuarios); // Mesmo vazio, retorna lista
});

// CADASTRAR USUÁRIO
app.MapPost("/api/usuario/cadastrar", async (Usuario usuario, AppDataContext banco) =>
{
    if (string.IsNullOrWhiteSpace(usuario.Nome))
        return Results.BadRequest("O nome é obrigatório");

    banco.Usuarios.Add(usuario);
    await banco.SaveChangesAsync();

    return Results.Created($"/api/usuario/{usuario.Id}", usuario);
});

// BUSCAR POR ID
app.MapGet("/api/usuario/{id}", async (int id, AppDataContext banco) =>
{
    var usuario = await banco.Usuarios.FindAsync(id);
    return usuario is null ? Results.NotFound("Usuário não encontrado") : Results.Ok(usuario);
});

// EDITAR
app.MapPut("/api/usuario/{id}", async (int id, Usuario usuario, AppDataContext banco) =>
{
    var existente = await banco.Usuarios.FindAsync(id);
    if (existente is null)
        return Results.NotFound("Usuário não encontrado");

    existente.Nome = usuario.Nome;

    await banco.SaveChangesAsync();
    return Results.Ok(existente);
});


app.MapDelete("/api/usuario/{id}", async (int id, AppDataContext banco) =>
{
    var usuario = await banco.Usuarios.FindAsync(id);
    if (usuario is null)
        return Results.NotFound("Usuário não encontrado");

    banco.Usuarios.Remove(usuario);
    await banco.SaveChangesAsync();

    return Results.Ok(usuario);
});


app.Run();