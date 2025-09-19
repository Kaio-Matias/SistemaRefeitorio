// ApiRefeicoes/Data/ApiRefeicoesDbContext.cs

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
        // NOVA LINHA: Adiciona a entidade Dispositivo ao contexto
        public DbSet<Dispositivo> Dispositivos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegistroRefeicao>(entity =>
            {
                entity.Property(e => e.ValorRefeicao).HasColumnType("decimal(18, 2)");
            });

            // NOVA CONFIGURAÇÃO: Define a relação entre Dispositivo e Usuário
            modelBuilder.Entity<Dispositivo>(entity =>
            {
                // Define a chave estrangeira
                entity.HasOne(d => d.Usuario)
                    .WithMany(u => u.Dispositivos)
                    .HasForeignKey(d => d.UsuarioId);

                // Garante que não haverá o mesmo identificador de dispositivo duplicado para o mesmo usuário
                entity.HasIndex(d => new { d.UsuarioId, d.DeviceIdentifier }).IsUnique();
            });
        }
    }
}