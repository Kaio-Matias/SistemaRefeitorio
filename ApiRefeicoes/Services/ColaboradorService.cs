using ApiRefeicoes.Data;
using ApiRefeicoes.Dtos;
using ApiRefeicoes.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiRefeicoes.Services
{
    public class ColaboradorService : IColaboradorService
    {
        private readonly ApiRefeicoesDbContext _context;
        private readonly FaceApiService _faceApiService;
        private readonly ILogger<ColaboradorService> _logger;

        public ColaboradorService(ApiRefeicoesDbContext context, FaceApiService faceApiService, ILogger<ColaboradorService> logger)
        {
            _context = context;
            _faceApiService = faceApiService;
            _logger = logger;
        }

        // --- INÍCIO DA CORREÇÃO ---
        public async Task<ColaboradorResponseDto> UpdateColaboradorAsync(int id, UpdateColaboradorDto colaboradorDto, Stream imagemStream)
        {
            var colaborador = await _context.Colaboradores.FindAsync(id);
            if (colaborador == null)
            {
                _logger.LogWarning("Tentativa de atualizar colaborador com ID {Id}, mas não foi encontrado.", id);
                return null;
            }

            // Atualiza os dados de texto do colaborador
            colaborador.Nome = colaboradorDto.Nome;
            colaborador.CartaoPonto = colaboradorDto.CartaoPonto;
            colaborador.FuncaoId = colaboradorDto.FuncaoId;
            colaborador.DepartamentoId = colaboradorDto.DepartamentoId;
            colaborador.Ativo = colaboradorDto.Ativo;

            // Verifica se uma nova imagem foi enviada
            if (imagemStream != null && imagemStream.Length > 0)
            {
                _logger.LogInformation("Nova imagem recebida para o colaborador ID: {Id}. Recriando PersonId.", id);

                // 1. Lê a nova imagem para um array de bytes
                byte[] imagemBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imagemStream.CopyToAsync(memoryStream);
                    imagemBytes = memoryStream.ToArray();
                }

                // 2. Cria uma nova "pessoa" na Azure para gerar um novo PersonId.
                //    Isso é necessário pois a API de Face não permite substituir a face principal, apenas adicionar novas.
                //    Criar uma nova pessoa garante que apenas a foto mais recente será usada para verificação.
                Guid newPersonId = await _faceApiService.CreatePersonAsync(colaboradorDto.Nome);
                if (newPersonId == Guid.Empty)
                {
                    _logger.LogError("Falha ao criar nova pessoa no serviço do Azure para o colaborador ID: {Id}", id);
                    throw new Exception("Não foi possível criar a nova pessoa no serviço do Azure para atualização.");
                }

                // 3. Adiciona a nova face à nova pessoa criada na Azure
                using (var streamParaAzure = new MemoryStream(imagemBytes))
                {
                    await _faceApiService.AddFaceToPersonAsync(newPersonId, streamParaAzure);
                }

                // 4. Atualiza a entidade Colaborador com os novos dados
                colaborador.Foto = imagemBytes;
                colaborador.PersonId = newPersonId;

                // 5. Aciona o retreinamento do grupo de reconhecimento facial
                _logger.LogInformation("Iniciando retreinamento do grupo após atualização da foto do colaborador ID: {Id}", id);
                await _faceApiService.TrainPersonGroupAsync();
            }

            _context.Colaboradores.Update(colaborador);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Colaborador ID {Id} salvo com sucesso no banco de dados.", id);

            // Busca os dados relacionados para retornar a resposta completa
            var funcao = await _context.Funcoes.FindAsync(colaborador.FuncaoId);
            var depto = await _context.Departamentos.FindAsync(colaborador.DepartamentoId);

            return new ColaboradorResponseDto
            {
                Id = colaborador.Id,
                Nome = colaborador.Nome,
                CartaoPonto = colaborador.CartaoPonto,
                Ativo = colaborador.Ativo,
                Foto = colaborador.Foto,
                Funcao = funcao?.Nome,
                Departamento = depto?.Nome,
                FuncaoId = colaborador.FuncaoId,
                DepartamentoId = colaborador.DepartamentoId
            };
        }
        // --- FIM DA CORREÇÃO ---


        public async Task<IEnumerable<ColaboradorResponseDto>> GetAllColaboradoresAsync(string? searchString, int? departamentoId, int? funcaoId, string? sortOrder)
        {
            var query = _context.Colaboradores
                .Include(c => c.Funcao)
                .Include(c => c.Departamento)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Nome.Contains(searchString) || c.CartaoPonto.Contains(searchString));
            }
            if (departamentoId.HasValue)
            {
                query = query.Where(c => c.DepartamentoId == departamentoId.Value);
            }
            if (funcaoId.HasValue)
            {
                query = query.Where(c => c.FuncaoId == funcaoId.Value);
            }
            switch (sortOrder)
            {
                case "nome_desc":
                    query = query.OrderByDescending(c => c.Nome);
                    break;
                default:
                    query = query.OrderBy(c => c.Nome);
                    break;
            }
            return await query.Select(c => new ColaboradorResponseDto
            {
                Id = c.Id,
                Nome = c.Nome,
                CartaoPonto = c.CartaoPonto,
                Ativo = c.Ativo,
                Foto = c.Foto,
                Funcao = c.Funcao.Nome,
                Departamento = c.Departamento.Nome,
                FuncaoId = c.FuncaoId,
                DepartamentoId = c.DepartamentoId
            }).ToListAsync();
        }

        // Os demais métodos (Create, GetById, etc.) permanecem como estão...
        public async Task<ColaboradorResponseDto> CreateColaboradorAsync(CreateColaboradorDto colaboradorDto, byte[] imagemBytes)
        {
            _logger.LogInformation("Iniciando processo de criação de colaborador no serviço.");
            await _faceApiService.EnsurePersonGroupExistsAsync();

            Guid personId = await _faceApiService.CreatePersonAsync(colaboradorDto.Nome);
            if (personId == Guid.Empty)
            {
                throw new Exception("Não foi possível criar a pessoa no serviço do Azure.");
            }

            using (var streamParaAzure = new MemoryStream(imagemBytes))
            {
                await _faceApiService.AddFaceToPersonAsync(personId, streamParaAzure);
            }

            var colaborador = new Colaborador
            {
                Nome = colaboradorDto.Nome,
                CartaoPonto = colaboradorDto.CartaoPonto,
                FuncaoId = colaboradorDto.FuncaoId,
                DepartamentoId = colaboradorDto.DepartamentoId,
                Ativo = true,
                PersonId = personId,
                Foto = imagemBytes
            };

            _context.Colaboradores.Add(colaborador);
            await _context.SaveChangesAsync();

            await _faceApiService.TrainPersonGroupAsync();

            var funcao = await _context.Funcoes.FindAsync(colaborador.FuncaoId);
            var depto = await _context.Departamentos.FindAsync(colaborador.DepartamentoId);

            return new ColaboradorResponseDto
            {
                Id = colaborador.Id,
                Nome = colaborador.Nome,
                CartaoPonto = colaborador.CartaoPonto,
                Ativo = colaborador.Ativo,
                Foto = colaborador.Foto,
                Funcao = funcao?.Nome,
                Departamento = depto?.Nome,
                FuncaoId = colaborador.FuncaoId,
                DepartamentoId = colaborador.DepartamentoId
            };
        }

        public async Task<ColaboradorResponseDto> GetColaboradorByIdAsync(int id)
        {
            var colaborador = await _context.Colaboradores
                                    .Include(c => c.Funcao)
                                    .Include(c => c.Departamento)
                                    .FirstOrDefaultAsync(c => c.Id == id);
            if (colaborador == null) return null;

            return new ColaboradorResponseDto
            {
                Id = colaborador.Id,
                Nome = colaborador.Nome,
                CartaoPonto = colaborador.CartaoPonto,
                Ativo = colaborador.Ativo,
                Foto = colaborador.Foto,
                Funcao = colaborador.Funcao.Nome,
                Departamento = colaborador.Departamento.Nome,
                FuncaoId = colaborador.FuncaoId,
                DepartamentoId = colaborador.DepartamentoId
            };
        }
    }
}