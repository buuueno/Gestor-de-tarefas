namespace GestorDeTarefas.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Relacionamento: lista de tarefas atribuídas ao usuário
        public List<Tarefa> Tarefas { get; set; } = new();
    }
}