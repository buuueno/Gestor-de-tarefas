namespace GestorDeTarefas.Models;

public class Tarefa
{
    public Tarefa()
    {
        CriadoEm = DateTime.Now;
        Id = Guid.NewGuid().ToString();
        Titulo = string.Empty;
    }

    // Corrija o tipo de Id para string se quiser usar Guid como string
    public string Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendente";
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateTime CriadoEm { get; set; } // Adicione esta linha
}
