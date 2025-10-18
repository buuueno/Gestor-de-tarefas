using GestorDeTarefas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;

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
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Configurando o contexto do banco de dados
builder.Services.AddDbContext<AppDataContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("String de conexão 'DefaultConnection' não encontrada.");
    }
    options.UseSqlite(connectionString);
    options.EnableSensitiveDataLogging();  // Habilita logs detalhados
    options.EnableDetailedErrors();        // Habilita erros detalhados
});

var app = builder.Build();

// Habilitando CORS
app.UseCors("AllowAll");

// Aplicar migrações pendentes automaticamente ao iniciar (garante que as tabelas existam)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDataContext>();
    db.Database.Migrate();
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
app.MapPost("/api/categoria/cadastrar", async (HttpContext context, [FromBody] Categoria categoria, [FromServices] AppDataContext banco, ILogger<Program> logger) =>
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
        logger.LogError(ex, "Erro ao cadastrar categoria: {Nome}", categoria.Nome);
        return Results.Problem("Erro ao cadastrar categoria. Verifique os logs para mais detalhes.");
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
app.MapGet("/api/tarefa/listar", ([FromServices] AppDataContext banco) =>
{
    if (banco.Tarefas.Any())
        return Results.Ok(banco.Tarefas.ToList());
    return Results.NotFound("Nenhuma tarefa encontrada.");
});

// Cadastrar tarefa
app.MapPost("/api/tarefa/cadastrar", ([FromBody] Tarefa tarefa, [FromServices] AppDataContext banco) =>
{
    Tarefa? resultado = banco.Tarefas.FirstOrDefault(x => x.Titulo == tarefa.Titulo);
    if (resultado is not null)
        return Results.Conflict("Tarefa já cadastrada");
    banco.Tarefas.Add(tarefa);
    banco.SaveChanges();
    return Results.Created("", tarefa);
});

// Buscar tarefa por título
app.MapGet("/api/tarefa/buscar/{titulo}", ([FromRoute] string titulo, [FromServices] AppDataContext banco) =>
{
    Tarefa? resultado = banco.Tarefas.FirstOrDefault(x => x.Titulo == titulo);
    if (resultado is null)
        return Results.NotFound("Tarefa não encontrada");
    return Results.Ok(resultado);
});

// Deletar tarefa
app.MapDelete("/api/tarefa/deletar/{id}", ([FromRoute] string id, [FromServices] AppDataContext banco) =>
{
    Tarefa? resultado = banco.Tarefas.Find(id);
    if (resultado == null)
        return Results.NotFound("Tarefa não encontrada.");
    banco.Tarefas.Remove(resultado);
    banco.SaveChanges();
    return Results.Ok(resultado);
});

// Alterar tarefa
app.MapPatch("/api/tarefa/alterar/{id}", ([FromRoute] string id, [FromBody] Tarefa tarefaAtualizada, [FromServices] AppDataContext banco) =>
{
    Tarefa? resultado = banco.Tarefas.Find(id);
    if (resultado == null)
        return Results.NotFound("Tarefa não encontrada.");
    resultado.Titulo = tarefaAtualizada.Titulo;
    resultado.Descricao = tarefaAtualizada.Descricao;
    resultado.Status = tarefaAtualizada.Status;
    resultado.CategoriaId = tarefaAtualizada.CategoriaId;
    banco.Tarefas.Update(resultado);
    banco.SaveChanges();
    return Results.Ok(resultado);
});



app.Run();