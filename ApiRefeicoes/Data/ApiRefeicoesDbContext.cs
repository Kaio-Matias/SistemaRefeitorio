using ApiRefeicoes.Models; // Usando a pasta de Models
using Microsoft.EntityFrameworkCore;

namespace ApiRefeicoes.Data
{
    public class ApiRefeicoesDbContext : DbContext
    {
        public ApiRefeicoesDbContext(DbContextOptions<ApiRefeicoesDbContext> options) : base(options)
        {
        }

        public DbSet<Colaborador> Colaboradores { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Funcao> Funcoes { get; set; }
        public DbSet<RegistroRefeicao> RegistrosRefeicoes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Adiciona uma constraint para garantir que o Cartão de Ponto seja único
            modelBuilder.Entity<Colaborador>()
                .HasIndex(c => c.CartaoPonto)
                .IsUnique();
        }
    }
}