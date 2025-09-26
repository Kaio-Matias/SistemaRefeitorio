using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ApiRefeicoes.Services;
using System.IO;
using ApiRefeicoes.Data;
using Microsoft.EntityFrameworkCore;
using System;
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

        /// <summary>
        /// Recebe uma foto do aplicativo, compara-a com todas as fotos de colaboradores no banco de dados
        /// e, se encontrar uma correspondência, regista a refeição.
        /// </summary>
        [HttpPost("registrar-ponto")]
        public async Task<IActionResult> RegistrarPonto([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Sucesso = false, Mensagem = "Nenhuma imagem foi enviada." });
            }

            _logger.LogInformation("Recebida requisição para registrar ponto. A processar a imagem do aplicativo...");

            using var memoryStreamApp = new MemoryStream();
            await file.CopyToAsync(memoryStreamApp);
            var imageBytesApp = memoryStreamApp.ToArray();

            var faceIdApp = await _faceApiService.DetectFace(imageBytesApp);
            if (faceIdApp == null)
            {
                return Ok(new { Sucesso = false, Mensagem = "Não foi possível detetar um rosto na imagem enviada." });
            }

            // ==================================================================
            // INÍCIO DA CORREÇÃO
            // ==================================================================
            // A condição foi alterada de c.Foto.Any() para c.Foto.Length > 0,
            // que é a forma correta de verificar um campo de imagem (byte[]) e
            // pode ser traduzida para SQL pelo Entity Framework.
            var colaboradoresComFoto = await _context.Colaboradores
                                                     .Where(c => c.Foto != null && c.Foto.Length > 0)
                                                     .ToListAsync();
            // ==================================================================
            // FIM DA CORREÇÃO
            // ==================================================================

            _logger.LogInformation("A iniciar comparação da face do app com {Count} colaboradores cadastrados.", colaboradoresComFoto.Count);

            foreach (var colaborador in colaboradoresComFoto)
            {
                _logger.LogInformation("A verificar colaborador: {Nome} (ID: {Id})", colaborador.Nome, colaborador.Id);

                var faceIdCadastro = await _faceApiService.DetectFace(colaborador.Foto);
                if (faceIdCadastro == null)
                {
                    _logger.LogWarning("Não foi possível detetar um rosto na foto de cadastro do colaborador ID {ColaboradorId}. A pular...", colaborador.Id);
                    continue;
                }

                var (isIdentical, confidence) = await _faceApiService.VerifyFaces(faceIdApp.Value, faceIdCadastro.Value);

                if (isIdentical && confidence > 0.5)
                {
                    _logger.LogInformation("SUCESSO: Colaborador {Nome} identificado com confiança de {Confidence}.", colaborador.Nome, confidence);

                    var registroRefeicao = new RegistroRefeicao
                    {
                        ColaboradorId = colaborador.Id,
                        HorarioRegistro = DateTime.Now
                    };

                    _context.RegistroRefeicoes.Add(registroRefeicao);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Refeição registrada com sucesso para o colaborador {Nome}.", colaborador.Nome);

                    return Ok(new
                    {
                        Sucesso = true,
                        Nome = colaborador.Nome,
                        FotoBase64 = colaborador.FotoBase64,
                        Mensagem = "Bem-vindo(a)!"
                    });
                }
            }

            _logger.LogWarning("Falha na identificação: Nenhum colaborador correspondente foi encontrado após comparar todos os registos.");
            return Ok(new { Sucesso = false, Mensagem = "Rosto não reconhecido." });
        }
    }
}