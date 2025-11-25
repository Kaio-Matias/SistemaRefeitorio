using ApiRefeicoes.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiRefeicoes.Data
{
    public class ApiRefeicoesDbContext : DbContext
    {
        public ApiRefeicoesDbContext(DbContextOptions<ApiRefeicoesDbContext> options) : base(options) { }

        public DbSet<Colaborador> Colaboradores { get; set; }
        public DbSet<Funcao> Funcoes { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<RegistroRefeicao> RegistroRefeicoes { get; set; }
        public DbSet<Justificativa> Justificativas { get; set; } // <--- Nova Tabela
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Dispositivo> Dispositivos { get; set; }
        public DbSet<ParadaDeFabrica> ParadasDeFabrica { get; set; }
        // public DbSet<Cardapio> Cardapios { get; set; } // Removido conforme migrações anteriores

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Colaborador>()
                .HasOne(c => c.Funcao)
                .WithMany(f => f.Colaboradores)
                .HasForeignKey(c => c.FuncaoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Colaborador>()
                .HasOne(c => c.Departamento)
                .WithMany(d => d.Colaboradores)
                .HasForeignKey(c => c.DepartamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegistroRefeicao>()
                .HasOne(r => r.Colaborador)
                .WithMany(c => c.RegistrosRefeicoes)
                .HasForeignKey(r => r.ColaboradorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuração do relacionamento Justificativa -> RegistroRefeicao
            modelBuilder.Entity<Justificativa>()
                .HasOne(j => j.RegistroRefeicao)
                .WithMany() // Um registro pode ter justificativas (geralmente 1-para-1 ou 1-para-N)
                .HasForeignKey(j => j.RegistroRefeicaoId)
                .OnDelete(DeleteBehavior.Cascade); // Se apagar a refeição, apaga a justificativa

            // Configuração de precisão decimal
            modelBuilder.Entity<RegistroRefeicao>()
                .Property(r => r.ValorRefeicao)
                .HasColumnType("decimal(17,0)");
        }
    }
}