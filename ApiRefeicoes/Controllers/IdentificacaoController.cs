using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using ApiRefeicoes.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ApiRefeicoes.Models;

namespace ApiRefeicoes.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class IdentificacaoController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ApiRefeicoesDbContext _context;
        private readonly ILogger<IdentificacaoController> _logger;

        public IdentificacaoController(FaceApiService faceApiService, ApiRefeicoesDbContext context, ILogger<IdentificacaoController> logger)
        {
            _faceApiService = faceApiService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("registrar-ponto")]
        public async Task<IActionResult> RegistrarPonto([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma imagem foi enviada." });
            }

            _logger.LogInformation("Requisição para registrar ponto recebida.");

            Guid? faceId;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reseta a posição do stream para o início
                faceId = await _faceApiService.DetectFace(memoryStream);
            }

            if (faceId == null)
            {
                return Ok(new { Sucesso = false, Mensagem = "Não foi possível detetar um rosto na imagem enviada." });
            }

            var personId = await _faceApiService.IdentifyFaceAsync(faceId.Value);

            if (personId == null)
            {
                _logger.LogWarning("Falha na identificação: Nenhum colaborador correspondente foi encontrado no grupo de pessoas.");
                return Ok(new { Sucesso = false, Mensagem = "Rosto não reconhecido." });
            }

            var colaborador = await _context.Colaboradores.FirstOrDefaultAsync(c => c.PersonId == personId.Value);

            if (colaborador == null)
            {
                _logger.LogError("INCONSISTÊNCIA DE DADOS: PersonId {PersonId} retornado pelo Azure mas não encontrado no banco de dados.", personId.Value);
                return StatusCode(500, new { Sucesso = false, Mensagem = "Erro de consistência nos dados. Contate o administrador." });
            }

            _logger.LogInformation("SUCESSO: Colaborador {Nome} identificado.", colaborador.Nome);

            var registroRefeicao = new RegistroRefeicao
            {
                ColaboradorId = colaborador.Id,
                DataHoraRegistro = DateTime.Now, // Corrigido de HorarioRegistro
                ValorRefeicao = 17, // Exemplo
                TipoRefeicao = "Almoço", // Exemplo
                ParadaDeFabrica = false // Exemplo
            };
            _context.RegistroRefeicoes.Add(registroRefeicao);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Sucesso = true,
                Nome = colaborador.Nome,
                FotoBase64 = colaborador.Foto != null ? Convert.ToBase64String(colaborador.Foto) : null,
                Mensagem = "Bem-vindo(a)!"
            });
        }
    }
}