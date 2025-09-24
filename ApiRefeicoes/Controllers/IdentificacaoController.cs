using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authorization;

namespace ApiRefeicoes.Controllers
{
    [Authorize] // Protege todo o controlador, exigindo um token válido
    [Route("api/[controller]")]
    [ApiController]
    public class IdentificacaoController : ControllerBase
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly FaceApiService _faceApiService;
        private readonly ILogger<IdentificacaoController> _logger;

        public IdentificacaoController(ApiRefeicoesDbContext context, FaceApiService faceApiService, ILogger<IdentificacaoController> logger)
        {
            _context = context;
            _faceApiService = faceApiService;
            _logger = logger;
        }

        [HttpPost("identificar")]
        public async Task<IActionResult> IdentificarColaborador(IFormFile fotoFile)
        {
            if (fotoFile == null || fotoFile.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma foto foi enviada." });
            }

            using var memoryStream = new MemoryStream();
            await fotoFile.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var azureIdDetectado = await _faceApiService.DetectFaceAndGetId(imageBytes);

            // ==================================================================
            // INÍCIO DA LINHA DE LOG PARA DEPURAÇÃO
            // ==================================================================
            _logger.LogInformation("Azure ID detectado pela API de Face: {AzureId}", string.IsNullOrEmpty(azureIdDetectado) ? "NENHUM" : azureIdDetectado);
            // ==================================================================
            // FIM DA LINHA DE LOG
            // ==================================================================

            if (string.IsNullOrEmpty(azureIdDetectado))
            {
                _logger.LogWarning("Nenhum rosto foi detectado na imagem enviada.");
                return NotFound(new { Sucesso = false, Mensagem = "Rosto não reconhecido." });
            }

            var colaborador = await _context.Colaboradores
                                            .FirstOrDefaultAsync(c => c.AzureId == azureIdDetectado);

            if (colaborador == null)
            {
                _logger.LogWarning("AzureId {AzureId} foi detectado mas não corresponde a nenhum colaborador no banco.", azureIdDetectado);
                return NotFound(new { Sucesso = false, Mensagem = "Colaborador não cadastrado." });
            }

            try
            {
                var novoRegistro = new RegistroRefeicao
                {
                    ColaboradorId = colaborador.Id,
                    HorarioRegistro = DateTime.Now,
                    ValorRefeicao = 15.0m // Pode ajustar este valor conforme necessário
                };
                _context.RegistrosRefeicoes.Add(novoRegistro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refeição registrada com sucesso para o colaborador: {Nome}", colaborador.Nome);

                string fotoBase64 = null;
                if (colaborador.Foto != null && colaborador.Foto.Length > 0)
                {
                    fotoBase64 = Convert.ToBase64String(colaborador.Foto);
                }

                return Ok(new
                {
                    Sucesso = true,
                    Mensagem = "Bem-vindo(a)!",
                    Nome = colaborador.Nome,
                    FotoBase64 = fotoBase64
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao guardar o registro da refeição para o colaborador {Nome}", colaborador.Nome);
                return StatusCode(500, new { Sucesso = false, Mensagem = "Ocorreu um erro interno ao registrar a refeição." });
            }
        }
    }
}