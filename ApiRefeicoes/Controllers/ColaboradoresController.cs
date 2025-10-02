using Microsoft.AspNetCore.Mvc;
using ApiRefeicoes.Services;
using ApiRefeicoes.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ApiRefeicoes.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ColaboradoresController : ControllerBase
    {
        private readonly IColaboradorService _colaboradorService;
        private readonly ILogger<ColaboradoresController> _logger;

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
                // A lógica de update pode ser ajustada da mesma forma se necessário
                using var stream = imagem?.OpenReadStream();
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
            _logger.LogInformation("--- INICIANDO CADASTRO DE COLABORADOR ---");
            // Logs...

            if (imagem == null || imagem.Length == 0)
            {
                return BadRequest("A imagem do colaborador é obrigatória.");
            }

            try
            {
                // --- INÍCIO DA CORREÇÃO DEFINITIVA ---
                // 1. Lê a imagem para um array de bytes UMA ÚNICA VEZ.
                byte[] imagemBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imagem.CopyToAsync(memoryStream);
                    imagemBytes = memoryStream.ToArray();
                }

                // 2. Passa o array de bytes para o serviço.
                _logger.LogInformation("Chamando o serviço IColaboradorService para criar o colaborador...");
                var novoColaborador = await _colaboradorService.CreateColaboradorAsync(colaboradorDto, imagemBytes);
                _logger.LogInformation("Colaborador criado com sucesso no serviço. Id={Id}", novoColaborador.Id);

                return CreatedAtAction(nameof(GetColaborador), new { id = novoColaborador.Id }, novoColaborador);
                // --- FIM DA CORREÇÃO DEFINITIVA ---
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uma exceção ocorreu ao tentar salvar o colaborador. Mensagem: {ErrorMessage}", ex.Message);
                return StatusCode(500, $"Erro interno ao criar colaborador. Verifique os logs da API para mais detalhes.");
            }
        }
    }
}