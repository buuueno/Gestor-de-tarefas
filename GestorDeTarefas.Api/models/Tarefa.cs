using System;

namespace GestorDeTarefas.Models
{
    public class Tarefa
    {
        public Tarefa()
        {
            CriadoEm = DateTime.UtcNow;
            Status = "Pendente";
            Titulo = string.Empty;
            Descricao = string.Empty;
        }

        public int Id { get; set; }                 
        public string Titulo { get; set; }          
        public string Descricao { get; set; }
        public string Status { get; set; }

       
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        public int UsuarioId { get; set; }         
        public Usuario? Usuario { get; set; }

        public DateTime CriadoEm { get; set; }
    }
}