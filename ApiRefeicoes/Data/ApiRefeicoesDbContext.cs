using ApiRefeicoes.Models;
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

        // ADICIONE ESTE MÉTODO
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegistroRefeicao>(entity =>
            {
                // Isso define o tipo da coluna no SQL Server para aceitar valores como 15.50
                entity.Property(e => e.ValorRefeicao).HasColumnType("decimal(18, 2)");
            });
        }
    }
}