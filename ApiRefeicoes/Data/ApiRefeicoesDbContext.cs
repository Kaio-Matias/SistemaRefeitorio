using ApiRefeicoes.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiRefeicoes.Data
{
    public class ApiRefeicoesDbContext : DbContext
    {
        public ApiRefeicoesDbContext(DbContextOptions<ApiRefeicoesDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Funcao> Funcoes { get; set; }
        public DbSet<Colaborador> Colaboradores { get; set; }
        public DbSet<Cardapio> Cardapios { get; set; }
        public DbSet<Dispositivo> Dispositivos { get; set; }
        public DbSet<RegistroRefeicao> RegistroRefeicoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ADICIONADO: Define o tipo da coluna para decimal, resolvendo o aviso.
            modelBuilder.Entity<RegistroRefeicao>()
                .Property(p => p.ValorRefeicao)
                .HasColumnType("decimal(18, 2)");
        }
    }
}