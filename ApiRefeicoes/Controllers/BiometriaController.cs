// ApiRefeicoes/Controllers/BiometriaController.cs
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using CIDBio; // Adicione o using para o SDK do iDBIO

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BiometriaController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;

        public BiometriaController(ApiRefeicoesDbContext context)
        {
            _context = context;
        }

        public class CadastroBiometriaRequest
        {
            public int ColaboradorId { get; set; }
            public string BiometriaTemplateBase64 { get; set; }
        }

        public class IdentificacaoBiometriaRequest
        {
            public string BiometriaTemplateBase64 { get; set; }
        }

        [HttpPost("cadastrar")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CadastrarBiometria([FromBody] CadastroBiometriaRequest request)
        {
            var colaborador = await _context.Colaboradores.FindAsync(request.ColaboradorId);
            if (colaborador == null)
            {
                return NotFound("Colaborador não encontrado.");
            }

            colaborador.BiometriaTemplate = Convert.FromBase64String(request.BiometriaTemplateBase64);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Biometria do colaborador cadastrada com sucesso." });
        }

        [HttpPost("identificar-e-registrar")]
        [AllowAnonymous]
        public async Task<IActionResult> IdentificarERegistrar([FromBody] IdentificacaoBiometriaRequest request)
        {
            var templateParaIdentificarBase64 = request.BiometriaTemplateBase64;

            var colaboradoresComBiometria = await _context.Colaboradores
                .Where(c => c.BiometriaTemplate != null && c.Ativo)
                .AsNoTracking()
                .ToListAsync();

            if (!colaboradoresComBiometria.Any())
            {
                return NotFound(new { Sucesso = false, Mensagem = "Nenhum colaborador com biometria cadastrada." });
            }

            Colaborador colaboradorIdentificado = null;

            // --- Lógica de Comparação 1-para-N com o SDK iDBIO ---
            // AVISO: Esta lógica deve ser executada no servidor onde a API está rodando.
            // É crucial que a DLL `libcidbio.dll` esteja presente junto com a aplicação.
            try
            {
                CIDBio.Init(); // Inicializa a biblioteca para usar a função de match

                foreach (var colaborador in colaboradoresComBiometria)
                {
                    string templateCadastradoBase64 = Convert.ToBase64String(colaborador.BiometriaTemplate);

                    // Utiliza a função MatchTemplates do SDK
                    var ret = CIDBio.MatchTemplates(templateParaIdentificarBase64, templateCadastradoBase64, out int score);

                    // A documentação define um "THRESHOLD" (limiar de semelhança).
                    // O valor padrão é automático, mas um valor comum é por volta de 8000-10000.
                    // Você pode precisar ajustar este valor.
                    if (ret == RetCode.SUCCESS && score > 8000)
                    {
                        colaboradorIdentificado = colaborador;
                        break; // Encontrou, pode parar o loop
                    }
                }
            }
            catch (Exception ex)
            {
                // Logar o erro
                return StatusCode(500, new { Sucesso = false, Mensagem = "Erro no SDK de biometria: " + ex.Message });
            }
            finally
            {
                CIDBio.Terminate(); // Garante que a biblioteca seja finalizada
            }
            // --- Fim da Lógica de Comparação ---

            if (colaboradorIdentificado == null)
            {
                return NotFound(new { Sucesso = false, Mensagem = "Digital não reconhecida." });
            }

            // Carrega os dados de navegação para o registro
            var colaboradorCompleto = await _context.Colaboradores
                                                      .Include(c => c.Departamento)
                                                      .Include(c => c.Funcao)
                                                      .FirstOrDefaultAsync(c => c.Id == colaboradorIdentificado.Id);

            var tipoRefeicao = DeterminarTipoRefeicao();
            var registro = new RegistroRefeicao
            {
                ColaboradorId = colaboradorCompleto.Id,
                DataHoraRegistro = DateTime.UtcNow,
                TipoRefeicao = tipoRefeicao,
                NomeColaborador = colaboradorCompleto.Nome,
                NomeDepartamento = colaboradorCompleto.Departamento?.Nome ?? "N/A",
                DepartamentoGenerico = colaboradorCompleto.Departamento?.DepartamentoGenerico,
                NomeFuncao = colaboradorCompleto.Funcao?.Nome ?? "N/A",
                ValorRefeicao = 17,
                ParadaDeFabrica = false
            };

            _context.RegistroRefeicoes.Add(registro);
            await _context.SaveChangesAsync();

            return Ok(new { Sucesso = true, Mensagem = $"{tipoRefeicao} registrada para {colaboradorCompleto.Nome}." });
        }

        private string DeterminarTipoRefeicao()
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            var horaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Hour;

            if (horaLocal >= 6 && horaLocal < 11) return "Café da Manhã";
            if (horaLocal >= 11 && horaLocal < 18) return "Almoço";
            if (horaLocal >= 18 && horaLocal < 22) return "Janta";
            return "Ceia";
        }
    }
}