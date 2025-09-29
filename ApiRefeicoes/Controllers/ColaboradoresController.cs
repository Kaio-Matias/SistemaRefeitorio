using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using ApiRefeicoes.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.Extensions.Logging; // Adicionado para logging

namespace ApiRefeicoes.Controllers
{
    [Authorize] // A segurança está reativada
    [Route("api/[controller]")]
    [ApiController]
    public class ColaboradoresController : ControllerBase
    {
        private readonly IColaboradorService _colaboradorService;
        private readonly ILogger<ColaboradoresController> _logger; // Injetando o logger

        public ColaboradoresController(IColaboradorService colaboradorService, ILogger<ColaboradoresController> logger)
        {
            _colaboradorService = colaboradorService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ColaboradorResponseDto>>> GetColaboradores()
        {
            var colaboradores = await _colaboradorService.GetAllColaboradoresAsync();
            return Ok(colaboradores);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ColaboradorResponseDto>> GetColaborador(int id)
        {
            var colaborador = await _colaboradorService.GetColaboradorByIdAsync(id);
            if (colaborador == null)
            {
                return NotFound();
            }
            return Ok(colaborador);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> PutColaborador(int id, [FromForm] UpdateColaboradorDto colaboradorDto, IFormFile? imagem)
        {
            _logger.LogInformation("Recebida requisição para ATUALIZAR colaborador ID: {Id}", id);
            try
            {
                // Usa um stream nulo se nenhuma imagem for enviada
                var stream = imagem?.OpenReadStream();
                var colaboradorAtualizado = await _colaboradorService.UpdateColaboradorAsync(id, colaboradorDto, stream);

                if (colaboradorAtualizado == null)
                {
                    _logger.LogWarning("Colaborador com ID {Id} não encontrado para atualização.", id);
                    return NotFound($"Colaborador com ID {id} não encontrado.");
                }

                _logger.LogInformation("Colaborador ID {Id} atualizado com sucesso.", id);
                return Ok(colaboradorAtualizado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao tentar atualizar o colaborador ID: {Id}", id);
                return StatusCode(500, $"Erro interno ao atualizar colaborador. Detalhes: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<ActionResult<ColaboradorResponseDto>> PostColaborador([FromForm] CreateColaboradorDto colaboradorDto, IFormFile imagem)
        {
            // --- INÍCIO DOS LOGS ---
            _logger.LogInformation("--- INICIANDO CADASTRO DE COLABORADOR ---");

            // 1. Verifica se o token de autorização foi recebido
            if (Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogInformation("Cabeçalho de autorização recebido: {AuthorizationHeader}", Request.Headers["Authorization"]);
            }
            else
            {
                _logger.LogWarning("NENHUM CABEÇALHO DE AUTORIZAÇÃO FOI RECEBIDO NA REQUISIÇÃO.");
                return Unauthorized("Cabeçalho de autorização ausente.");
            }

            // 2. Log dos dados recebidos
            _logger.LogInformation("Dados recebidos do formulário: Nome={Nome}, CartaoPonto={CartaoPonto}, FuncaoId={FuncaoId}, DepartamentoId={DepartamentoId}",
                colaboradorDto.Nome, colaboradorDto.CartaoPonto, colaboradorDto.FuncaoId, colaboradorDto.DepartamentoId);

            if (imagem == null || imagem.Length == 0)
            {
                _logger.LogError("Erro de validação: A imagem do colaborador é obrigatória e não foi enviada.");
                return BadRequest("A imagem do colaborador é obrigatória.");
            }
            _logger.LogInformation("Imagem recebida: FileName={FileName}, ContentType={ContentType}, Length={Length} bytes",
                imagem.FileName, imagem.ContentType, imagem.Length);
            // --- FIM DOS LOGS ---

            try
            {
                using (var stream = imagem.OpenReadStream())
                {
                    _logger.LogInformation("Chamando o serviço IColaboradorService para criar o colaborador...");
                    var novoColaborador = await _colaboradorService.CreateColaboradorAsync(colaboradorDto, stream);
                    _logger.LogInformation("Colaborador criado com sucesso no serviço. Id={Id}", novoColaborador.Id);
                    return CreatedAtAction(nameof(GetColaborador), new { id = novoColaborador.Id }, novoColaborador);
                }
            }
            catch (Exception ex)
            {
                // Captura e loga QUALQUER erro que aconteça na camada de serviço ou no banco de dados
                _logger.LogError(ex, "Uma exceção ocorreu ao tentar salvar o colaborador. Mensagem: {ErrorMessage}", ex.Message);
                return StatusCode(500, $"Erro interno ao criar colaborador. Verifique os logs da API para mais detalhes.");
            }
        }
    }
}