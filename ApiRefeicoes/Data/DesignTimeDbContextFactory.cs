using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ApiRefeicoes.Data
{
    /// <summary>
    /// Esta classe é usada pelas ferramentas do Entity Framework Core (como para criar migrations)
    /// para criar uma instância do DbContext em tempo de design.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApiRefeicoesDbContext>
    {
        public ApiRefeicoesDbContext CreateDbContext(string[] args)
        {
            // Constrói o objeto de configuração para ler o appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<ApiRefeicoesDbContext>();

            // Obtém a string de conexão do arquivo de configuração
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configura o DbContext para usar o SQL Server com a string de conexão obtida
            builder.UseSqlServer(connectionString);

            // Retorna a nova instância do DbContext configurada
            return new ApiRefeicoesDbContext(builder.Options);
        }
    }
}
