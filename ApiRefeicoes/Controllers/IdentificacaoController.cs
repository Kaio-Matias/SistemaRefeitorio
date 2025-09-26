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
using System.Linq; // Adicione este using para o .Where() em memória

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

            _logger.LogInformation("Requisição para registar ponto recebida.");

            using var memoryStreamApp = new MemoryStream();
            await file.CopyToAsync(memoryStreamApp);
            var imageBytesApp = memoryStreamApp.ToArray();

            var faceIdApp = await _faceApiService.DetectFace(imageBytesApp);
            if (faceIdApp == null)
            {
                return Ok(new { Sucesso = false, Mensagem = "Não foi possível detetar um rosto na imagem enviada." });
            }

            // ==================================================================
            // SOLUÇÃO DEFINITIVA: AVALIAÇÃO NO LADO DO CLIENTE
            // 1. Primeiro, trazemos todos os colaboradores para a memória.
            var todosOsColaboradores = await _context.Colaboradores.ToListAsync();

            // 2. DEPOIS, filtramos a lista na memória da aplicação.
            //    Isto evita o erro de tradução do LINQ para SQL.
            var colaboradoresComFoto = todosOsColaboradores
                                           .Where(c => c.Foto != null && c.Foto.Length > 0)
                                           .ToList();
            // ==================================================================

            _logger.LogInformation("A comparar com {Count} colaboradores com foto.", colaboradoresComFoto.Count);

            foreach (var colaborador in colaboradoresComFoto)
            {
                var faceIdCadastro = await _faceApiService.DetectFace(colaborador.Foto);
                if (faceIdCadastro == null)
                {
                    _logger.LogWarning("A foto do colaborador {Nome} não tem um rosto detetável.", colaborador.Nome);
                    continue;
                }

                var (isIdentical, confidence) = await _faceApiService.VerifyFaces(faceIdApp.Value, faceIdCadastro.Value);
                _logger.LogInformation("A verificar {Nome}: Idêntico={isIdentical}, Confiança={confidence}", colaborador.Nome, isIdentical, confidence);

                if (isIdentical && confidence > 0.6)
                {
                    _logger.LogInformation("SUCESSO: Colaborador {Nome} identificado.", colaborador.Nome);
                    var registroRefeicao = new RegistroRefeicao
                    {
                        ColaboradorId = colaborador.Id,
                        HorarioRegistro = DateTime.Now,
                        ValorRefeicao = 17,
                        Dispositivo = "Aplicação Móvel"
                    };
                    _context.RegistroRefeicoes.Add(registroRefeicao);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        Sucesso = true,
                        Nome = colaborador.Nome,
                        FotoBase64 = Convert.ToBase64String(colaborador.Foto),
                        Mensagem = "Bem-vindo(a)!"
                    });
                }
            }

            _logger.LogWarning("Falha na identificação: Nenhum colaborador correspondente foi encontrado.");
            return Ok(new { Sucesso = false, Mensagem = "Rosto não reconhecido." });
        }
    }
}