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
        public DbSet<Usuario> Usuarios { get; set; }
       
        public DbSet<Dispositivo> Dispositivos { get; set; }
        public DbSet<ParadaDeFabrica> ParadasDeFabrica { get; set; }
        public DbSet<RegistroRefeicao> RegistroRefeicoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Agora essas configurações funcionarão sem erros
            modelBuilder.Entity<Colaborador>()
                .HasOne(c => c.Funcao)
                .WithMany(f => f.Colaboradores)
                .HasForeignKey(c => c.FuncaoId);

            modelBuilder.Entity<Colaborador>()
                .HasOne(c => c.Departamento)
                .WithMany(d => d.Colaboradores)
                .HasForeignKey(c => c.DepartamentoId);

            modelBuilder.Entity<RegistroRefeicao>()
                .HasOne(r => r.Colaborador)
                .WithMany(c => c.RegistrosRefeicoes)
                .HasForeignKey(r => r.ColaboradorId);
        }
    }
}