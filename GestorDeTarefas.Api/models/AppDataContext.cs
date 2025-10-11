using Microsoft.EntityFrameworkCore;

namespace GestorDeTarefas.Models
{
    public class AppDataContext : DbContext
    {
        public DbSet<Tarefa> Tarefas { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=GestorTarefas.db");
        }
    }
}