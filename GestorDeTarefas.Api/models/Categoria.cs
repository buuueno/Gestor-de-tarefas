using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GestorDeTarefas.Models
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        // evita ciclo de referência ao serializar
        [JsonIgnore]
        public List<Tarefa> Tarefas { get; set; } = new();
    }
}