using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiRefeicoes.Data;
using ApiRefeicoes.Models;
using ApiRefeicoes.Services;

namespace ApiRefeicoes.Controllers
{
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
                return BadRequest(new { message = "Nenhuma foto foi enviada." });
            }

            using var memoryStream = new MemoryStream();
            await fotoFile.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var azureIdDetectado = await _faceApiService.DetectFaceAndGetId(imageBytes);

            if (string.IsNullOrEmpty(azureIdDetectado))
            {
                return NotFound(new { message = "Nenhum rosto foi detetado na imagem." });
            }

            var colaborador = await _context.Colaboradores
                                            .FirstOrDefaultAsync(c => c.AzureId == azureIdDetectado);

            if (colaborador == null)
            {
                _logger.LogWarning("AzureId {AzureId} foi detetado mas não corresponde a nenhum colaborador no banco.", azureIdDetectado);
                return NotFound(new { message = "Colaborador não encontrado ou não registado no sistema." });
            }

            try
            {
                var novoRegistro = new RegistroRefeicao
                {
                    ColaboradorId = colaborador.Id,
                    HorarioRegistro = DateTime.Now,
                    ValorRefeicao = 15.0m // Valor de exemplo
                };
                _context.RegistrosRefeicoes.Add(novoRegistro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refeição registada com sucesso para o colaborador: {Nome}", colaborador.Nome);
                return Ok(new { message = "Refeição registada com sucesso!", colaboradorNome = colaborador.Nome });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao guardar o registo da refeição para o colaborador {Nome}", colaborador.Nome);
                return StatusCode(500, new { message = "Ocorreu um erro interno ao registar a refeição." });
            }
        }
    }
}