using GestorDeTarefas.Models;
using GestorDeTarefas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Configurando CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", 
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Configurando Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurando JSON para evitar referência circular
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Configurando o contexto do banco de dados (USANDO arquivo SQLite local)
builder.Services.AddDbContext<AppDataContext>(options =>
{
    options.UseSqlite("Data Source=GestorTarefas.db");
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
});

var app = builder.Build();

// Habilitando CORS
app.UseCors("AllowAll");

// Aplicar migrações pendentes automaticamente ao iniciar (garante que as tabelas existam)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDataContext>();
    // Em dev: cria o esquema se não existir (não usa migrations)
    db.Database.EnsureCreated();
}

// Configuração do pipeline HTTP
app.UseExceptionHandler("/error");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Configurando Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configurando logs
app.Logger.LogInformation("Iniciando a aplicação...");

// Endpoint para debugging
app.MapGet("/error", (HttpContext httpContext) =>
{
    var exceptionHandler = httpContext.Features.Get<IExceptionHandlerPathFeature>();
    return Results.Problem(
        detail: exceptionHandler?.Error?.StackTrace,
        title: exceptionHandler?.Error?.Message);
});


// Endpoints de Tarefas

app.MapGet("/", () => "api de tarefas e categorias");

// Endpoints de Categoria

// Listar categorias
app.MapGet("/api/categoria/listar", ([FromServices] AppDataContext banco) =>
{
    if (banco.Categorias.Any())
        return Results.Ok(banco.Categorias.Include(c => c.Tarefas).ToList());
    return Results.NotFound("Nenhuma categoria encontrada.");
});

// Cadastrar categoria
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

// Buscar categoria por ID
app.MapGet("/api/categoria/{id}", ([FromRoute] int id, [FromServices] AppDataContext banco) =>
{
    var categoria = banco.Categorias.Include(c => c.Tarefas).FirstOrDefault(x => x.Id == id);
    if (categoria is null)
        return Results.NotFound("Categoria não encontrada");
    return Results.Ok(categoria);
});

// Atualizar categoria
app.MapPut("/api/categoria/{id}", ([FromRoute] int id, [FromBody] Categoria categoria, [FromServices] AppDataContext banco) =>
{
    var categoriaExistente = banco.Categorias.Find(id);
    if (categoriaExistente is null)
        return Results.NotFound("Categoria não encontrada");
    
    categoriaExistente.Nome = categoria.Nome;
    banco.SaveChanges();
    return Results.Ok(categoriaExistente);
});

// Deletar categoria
app.MapDelete("/api/categoria/{id}", ([FromRoute] int id, [FromServices] AppDataContext banco) =>
{
    var categoria = banco.Categorias.Find(id);
    if (categoria is null)
        return Results.NotFound("Categoria não encontrada");
    
    banco.Categorias.Remove(categoria);
    banco.SaveChanges();
    return Results.Ok(categoria);
});

// Listar tarefas
app.MapGet("/api/tarefa/listar", async ([FromServices] AppDataContext banco) =>
{
    var tarefas = await banco.Tarefas
        .Include(t => t.Categoria)
        .Include(t => t.Usuario)
        .ToListAsync();
    return tarefas.Count == 0 ? Results.NotFound("Nenhuma tarefa encontrada.") : Results.Ok(tarefas);
});

// Buscar tarefa por título
app.MapGet("/api/tarefa/buscar/{titulo}", async ([FromRoute] string titulo, [FromServices] AppDataContext banco) =>
{
    var tarefa = await banco.Tarefas
        .Include(t => t.Categoria)
        .Include(t => t.Usuario)
        .FirstOrDefaultAsync(t => t.Titulo == titulo);
    return tarefa is null ? Results.NotFound("Tarefa não encontrada.") : Results.Ok(tarefa);
});

// Cadastrar tarefa (valida categoria e usuario)
app.MapPost("/api/tarefa/cadastrar", async ([FromBody] Tarefa tarefa, [FromServices] AppDataContext banco) =>
{
    if (string.IsNullOrWhiteSpace(tarefa.Titulo))
        return Results.BadRequest("Título obrigatório.");

    var categoria = await banco.Categorias.FindAsync(tarefa.CategoriaId);
    if (categoria is null)
        return Results.BadRequest($"Categoria com id {tarefa.CategoriaId} não encontrada.");

    var usuario = await banco.Usuarios.FindAsync(tarefa.UsuarioId);
    if (usuario is null)
        return Results.BadRequest($"Usuário com id {tarefa.UsuarioId} não encontrado.");

    tarefa.Categoria = categoria;
    tarefa.Usuario = usuario;

    banco.Tarefas.Add(tarefa);
    await banco.SaveChangesAsync();

    // incluir relações no retorno
    await banco.Entry(tarefa).Reference(t => t.Categoria).LoadAsync();
    await banco.Entry(tarefa).Reference(t => t.Usuario).LoadAsync();

    return Results.Created($"/api/tarefa/{tarefa.Id}", tarefa);
});

// Atualizar tarefa por id
app.MapPatch("/api/tarefa/{id}", async ([FromRoute] int id, [FromBody] Tarefa atual, [FromServices] AppDataContext banco) =>
{
    var tarefa = await banco.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound("Tarefa não encontrada.");

    if (!string.IsNullOrWhiteSpace(atual.Titulo)) tarefa.Titulo = atual.Titulo;
    if (!string.IsNullOrWhiteSpace(atual.Descricao)) tarefa.Descricao = atual.Descricao;
    if (!string.IsNullOrWhiteSpace(atual.Status)) tarefa.Status = atual.Status;

    if (atual.CategoriaId != 0 && atual.CategoriaId != tarefa.CategoriaId)
    {
        var categoria = await banco.Categorias.FindAsync(atual.CategoriaId);
        if (categoria is null) return Results.BadRequest($"Categoria com id {atual.CategoriaId} não encontrada.");
        tarefa.CategoriaId = atual.CategoriaId;
        tarefa.Categoria = categoria;
    }

    if (atual.UsuarioId != 0 && atual.UsuarioId != tarefa.UsuarioId)
    {
        var usuario = await banco.Usuarios.FindAsync(atual.UsuarioId);
        if (usuario is null) return Results.BadRequest($"Usuário com id {atual.UsuarioId} não encontrado.");
        tarefa.UsuarioId = atual.UsuarioId;
        tarefa.Usuario = usuario;
    }

    banco.Tarefas.Update(tarefa);
    await banco.SaveChangesAsync();
    return Results.Ok(tarefa);
});

// Deletar tarefa
app.MapDelete("/api/tarefa/{id}", async ([FromRoute] int id, [FromServices] AppDataContext banco) =>
{
    var tarefa = await banco.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound("Tarefa não encontrada.");
    banco.Tarefas.Remove(tarefa);
    await banco.SaveChangesAsync();
    return Results.Ok(tarefa);
});

app.MapGet("/api/usuario/listar", async ([FromServices] AppDataContext banco) =>
{
    var usuarios = await banco.Usuarios.ToListAsync();
    return usuarios.Count == 0 ? Results.NotFound("Nenhum usuário encontrado.") : Results.Ok(usuarios);
});

// Buscar usuário por id (inclui tarefas)
app.MapGet("/api/usuario/{id}", async ([FromRoute] int id, [FromServices] AppDataContext banco) =>
{
    var usuario = await banco.Usuarios.Include(u => u.Tarefas).FirstOrDefaultAsync(u => u.Id == id);
    return usuario is null ? Results.NotFound("Usuário não encontrado.") : Results.Ok(usuario);
});

// Buscar usuário por nome
app.MapGet("/api/usuario/buscar/{nome}", async ([FromRoute] string nome, [FromServices] AppDataContext banco) =>
{
    var usuario = await banco.Usuarios.FirstOrDefaultAsync(u => u.Nome == nome);
    return usuario is null ? Results.NotFound("Usuário não encontrado") : Results.Ok(usuario);
});

// Cadastrar usuário
app.MapPost("/api/usuario/cadastrar", async ([FromBody] Usuario usuario, [FromServices] AppDataContext banco) =>
{
    if (string.IsNullOrWhiteSpace(usuario.Nome) || string.IsNullOrWhiteSpace(usuario.Email))
        return Results.BadRequest("Nome e email obrigatórios.");
    if (await banco.Usuarios.AnyAsync(u => u.Email == usuario.Email))
        return Results.Conflict("Usuário já cadastrado");
    banco.Usuarios.Add(usuario);
    await banco.SaveChangesAsync();
    return Results.Created($"/api/usuario/{usuario.Id}", usuario);
});

// Atualizar usuário (por id)
app.MapPatch("/api/usuario/{id}", async ([FromRoute] int id, [FromBody] Usuario usuarioAtualizado, [FromServices] AppDataContext banco) =>
{
    var usuario = await banco.Usuarios.FindAsync(id);
    if (usuario is null) return Results.NotFound("Usuário não encontrado.");
    usuario.Nome = string.IsNullOrWhiteSpace(usuarioAtualizado.Nome) ? usuario.Nome : usuarioAtualizado.Nome;
    usuario.Email = string.IsNullOrWhiteSpace(usuarioAtualizado.Email) ? usuario.Email : usuarioAtualizado.Email;
    banco.Usuarios.Update(usuario);
    await banco.SaveChangesAsync();
    return Results.Ok(usuario);
});

// Deletar usuário (por id)
app.MapDelete("/api/usuario/{id}", async ([FromRoute] int id, [FromServices] AppDataContext banco) =>
{
    var usuario = await banco.Usuarios.FindAsync(id);
    if (usuario is null) return Results.NotFound("Usuário não encontrado.");
    banco.Usuarios.Remove(usuario);
    await banco.SaveChangesAsync();
    return Results.Ok(usuario);
});


app.Run();