using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using ApiRefeicoes.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ApiRefeicoes.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.Extensions.Logging;

namespace ApiRefeicoes.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReconhecimentoController : ControllerBase
    {
        private readonly FaceApiService _faceApiService;
        private readonly ApiRefeicoesDbContext _context;
        private readonly ILogger<ReconhecimentoController> _logger;

        public ReconhecimentoController(FaceApiService faceApiService, ApiRefeicoesDbContext context, ILogger<ReconhecimentoController> logger)
        {
            _faceApiService = faceApiService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("identificar")]
        public async Task<IActionResult> Identificar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma imagem foi enviada." });
            }

            _logger.LogInformation("Requisição de identificação recebida.");

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

            // Método correto e performático para identificação
            var personId = await _faceApiService.IdentifyFaceAsync(faceId.Value);

            if (personId == null)
            {
                _logger.LogWarning("Falha na identificação: Nenhum colaborador correspondente foi encontrado.");
                return Ok(new { Sucesso = false, Mensagem = "Rosto não reconhecido." });
            }

            var colaborador = await _context.Colaboradores.FirstOrDefaultAsync(c => c.PersonId == personId.Value);

            if (colaborador == null)
            {
                _logger.LogError("INCONSISTÊNCIA DE DADOS: PersonId {PersonId} retornado pelo Azure mas não encontrado no banco de dados.", personId.Value);
                return StatusCode(500, new { Sucesso = false, Mensagem = "Erro de consistência nos dados. Contate o administrador." });
            }

            _logger.LogInformation("SUCESSO: Colaborador {Nome} identificado.", colaborador.Nome);

            // Aqui você pode adicionar a lógica para registrar a refeição, se necessário.

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