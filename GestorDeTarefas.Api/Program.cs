using GestorDeTarefas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDataContext>();
var app = builder.Build();

// Endpoints de Tarefas

app.MapGet("/", () => "api de tarefas e categorias");

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

// Endpoints de Categorias

// Listar categorias
app.MapGet("/api/categoria/listar", ([FromServices] AppDataContext banco) =>
{
    if (banco.Categorias.Any())
        return Results.Ok(banco.Categorias.ToList());
    return Results.NotFound("Nenhuma categoria encontrada.");
});

// Cadastrar categoria
app.MapPost("/api/categoria/cadastrar", ([FromBody] Categoria categoria, [FromServices] AppDataContext banco) =>
{
    Categoria? resultado = banco.Categorias.FirstOrDefault(x => x.Nome == categoria.Nome);
    if (resultado is not null)
        return Results.Conflict("Categoria já cadastrada");
    banco.Categorias.Add(categoria);
    banco.SaveChanges();
    return Results.Created("", categoria);
});

// Buscar categoria por nome
app.MapGet("/api/categoria/buscar/{nome}", ([FromRoute] string nome, [FromServices] AppDataContext banco) =>
{
    Categoria? resultado = banco.Categorias.FirstOrDefault(x => x.Nome == nome);
    if (resultado is null)
        return Results.NotFound("Categoria não encontrada");
    return Results.Ok(resultado);
});

// Deletar categoria
app.MapDelete("/api/categoria/deletar/{id}", ([FromRoute] string id, [FromServices] AppDataContext banco) =>
{
    Categoria? resultado = banco.Categorias.Find(id);
    if (resultado == null)
        return Results.NotFound("Categoria não encontrada.");
    banco.Categorias.Remove(resultado);
    banco.SaveChanges();
    return Results.Ok(resultado);
});

// Alterar categoria
app.MapPatch("/api/categoria/alterar/{id}", ([FromRoute] string id, [FromBody] Categoria categoriaAtualizada, [FromServices] AppDataContext banco) =>
{
    Categoria? resultado = banco.Categorias.Find(id);
    if (resultado == null)
        return Results.NotFound("Categoria não encontrada.");
    resultado.Nome = categoriaAtualizada.Nome;
    banco.Categorias.Update(resultado);
    banco.SaveChanges();
    return Results.Ok(resultado);
});

app.Run();