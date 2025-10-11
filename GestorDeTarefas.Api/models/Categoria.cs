namespace GestorDeTarefas.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime Criado { get; set; } = DateTime.Now;
    public List<Tarefa> Tarefas { get; set; } = new();
}