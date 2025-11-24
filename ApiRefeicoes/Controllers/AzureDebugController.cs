using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using ApiRefeicoes.Data;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiRefeicoes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")] // Garante que apenas Super Admins acessem essas ferramentas perigosas
    public class AzureDebugController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ApiRefeicoesDbContext _context;
        private readonly ILogger<AzureDebugController> _logger;

        public AzureDebugController(
            FaceApiService faceApiService,
            ApiRefeicoesDbContext context,
            ILogger<AzureDebugController> logger)
        {
            _faceApiService = faceApiService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Verifica o status do último treinamento.
        /// </summary>
        [HttpGet("training-status")]
        public async Task<ActionResult<object>> GetTrainingStatus()
        {
            try
            {
                var status = await _faceApiService.GetTrainingStatusAsync();
                return Ok(new
                {
                    Status = status.Status.ToString(),
                    Created = status.Created,
                    LastAction = status.LastAction,
                    Message = status.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao verificar o estado do treino.");
                return StatusCode(500, new { Message = "Erro ao verificar status.", Details = ex.Message });
            }
        }

        /// <summary>
        /// Obtém detalhes técnicos do PersonGroup no Azure.
        /// </summary>
        [HttpGet("group-details")]
        public async Task<IActionResult> GetGroupDetails()
        {
            try
            {
                var group = await _faceApiService.GetPersonGroupAsync();
                return Ok(group);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Erro ao obter detalhes do grupo.", Details = ex.Message });
            }
        }

        /// <summary>
        /// Força o treinamento manual do grupo.
        /// </summary>
        [HttpPost("force-train")]
        public async Task<IActionResult> ForceTrain()
        {
            try
            {
                _logger.LogInformation("Forçando o treino do grupo manualmente.");
                await _faceApiService.TrainPersonGroupAsync();
                return Ok(new { Message = "Treino iniciado e concluído com sucesso." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Erro ao treinar.", Details = ex.Message });
            }
        }

        /// <summary>
        /// ROTINA DE CURA: Apaga tudo no Azure e recadastra baseada no Banco de Dados local.
        /// Resolve o erro "PersonId existe no Azure mas não no banco local".
        /// </summary>
        [HttpPost("sincronizar-total")]
        [AllowAnonymous]
        public async Task<IActionResult> SincronizarBaseTotal()
        {
            // AVISO: Esta operação pode demorar. Não feche a conexão.
            try
            {
                _logger.LogWarning("!!! INICIANDO SINCRONIZAÇÃO TOTAL (DB -> AZURE) !!!");

                // 1. Resetar o ambiente no Azure (Apagar e Recriar Grupo)
                // Isso garante que não existam IDs "fantasmas" no Azure.
                await _faceApiService.DeletePersonGroupAsync();
                await _faceApiService.EnsurePersonGroupExistsAsync();

                // 2. Buscar todos os colaboradores ativos do banco local
                var colaboradores = await _context.Colaboradores
                                                  .Where(c => c.Ativo)
                                                  .ToListAsync();

                _logger.LogInformation($"Iniciando processamento de {colaboradores.Count} colaboradores.");

                int sucessos = 0;
                int erros = 0;
                int semFoto = 0;

                foreach (var colab in colaboradores)
                {
                    try
                    {
                        // 3. Criar a pessoa no Azure (Gera um NOVO PersonId)
                        var novoPersonId = await _faceApiService.CreatePersonAsync(colab.Nome);

                        // 4. Atualizar o PersonId no objeto local
                        colab.PersonId = novoPersonId;

                        // 5. Enviar a foto para o Azure (se existir)
                        if (colab.Foto != null && colab.Foto.Length > 0)
                        {
                            using (var stream = new MemoryStream(colab.Foto))
                            {
                                await _faceApiService.AddFaceToPersonAsync(novoPersonId, stream);
                            }
                            sucessos++;
                        }
                        else
                        {
                            _logger.LogWarning($"Colaborador {colab.Nome} (ID {colab.Id}) não possui foto. Criado no Azure apenas com nome.");
                            semFoto++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ($"Erro ao sincronizar colaborador {colab.Nome} (ID {colab.Id})."));
                        erros++;
                        // Não para o loop, tenta o próximo
                    }
                }

                // 6. Salvar todos os novos PersonIds no banco de dados local
                await _context.SaveChangesAsync();

                // 7. Treinar o grupo com os novos dados
                if (sucessos > 0)
                {
                    await _faceApiService.TrainPersonGroupAsync();
                }

                var resumo = new
                {
                    Mensagem = "Sincronização concluída com sucesso.",
                    TotalProcessado = colaboradores.Count,
                    CadastradosComFoto = sucessos,
                    CadastradosSemFoto = semFoto,
                    Falhas = erros,
                    Nota = "Os PersonIds no banco de dados foram atualizados. O reconhecimento deve funcionar agora."
                };

                _logger.LogInformation("Sincronização finalizada. {@Resumo}", resumo);

                return Ok(resumo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal durante a sincronização total.");
                return StatusCode(500, new { Mensagem = "Erro fatal na sincronização.", Detalhes = ex.Message });
            }
        }
    }
}