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
                Foto = imagemBytes,

                // --- MAPEAMENTO DAS NOVAS PERMISSÕES NA CRIAÇÃO ---
                AcessoCafeDaManha = colaboradorDto.AcessoCafeDaManha,
                AcessoAlmoco = colaboradorDto.AcessoAlmoco,
                AcessoJanta = colaboradorDto.AcessoJanta,
                AcessoCeia = colaboradorDto.AcessoCeia
            };

            _context.Colaboradores.Add(colaborador);
            await _context.SaveChangesAsync();
            await _faceApiService.TrainPersonGroupAsync();

            return await GetColaboradorByIdAsync(colaborador.Id);
        }

        public async Task<ColaboradorResponseDto> UpdateColaboradorAsync(int id, UpdateColaboradorDto colaboradorDto, Stream imagemStream)
        {
            var colaborador = await _context.Colaboradores.FindAsync(id);
            if (colaborador == null)
            {
                _logger.LogWarning("Tentativa de atualizar colaborador com ID {Id}, mas não foi encontrado.", id);
                return null;
            }

            colaborador.Nome = colaboradorDto.Nome;
            colaborador.CartaoPonto = colaboradorDto.CartaoPonto;
            colaborador.FuncaoId = colaboradorDto.FuncaoId;
            colaborador.DepartamentoId = colaboradorDto.DepartamentoId;
            colaborador.Ativo = colaboradorDto.Ativo;

            // --- ATUALIZAÇÃO DAS NOVAS PERMISSÕES ---
            colaborador.AcessoCafeDaManha = colaboradorDto.AcessoCafeDaManha;
            colaborador.AcessoAlmoco = colaboradorDto.AcessoAlmoco;
            colaborador.AcessoJanta = colaboradorDto.AcessoJanta;
            colaborador.AcessoCeia = colaboradorDto.AcessoCeia;

            if (imagemStream != null && imagemStream.Length > 0)
            {
                _logger.LogInformation("Nova imagem recebida para o colaborador ID: {Id}. Recriando PersonId.", id);
                byte[] imagemBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await imagemStream.CopyToAsync(memoryStream);
                    imagemBytes = memoryStream.ToArray();
                }

                Guid newPersonId = await _faceApiService.CreatePersonAsync(colaboradorDto.Nome);
                if (newPersonId == Guid.Empty)
                {
                    throw new Exception("Não foi possível criar a nova pessoa no serviço do Azure para atualização.");
                }

                using (var streamParaAzure = new MemoryStream(imagemBytes))
                {
                    await _faceApiService.AddFaceToPersonAsync(newPersonId, streamParaAzure);
                }

                colaborador.Foto = imagemBytes;
                colaborador.PersonId = newPersonId;

                await _faceApiService.TrainPersonGroupAsync();
            }

            _context.Colaboradores.Update(colaborador);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Colaborador ID {Id} salvo com sucesso no banco de dados.", id);

            return await GetColaboradorByIdAsync(id);
        }

        public async Task<IEnumerable<ColaboradorResponseDto>> GetAllColaboradoresAsync(string? searchString, int? departamentoId, int? funcaoId, string? sortOrder)
        {
            var query = _context.Colaboradores
                .Include(c => c.Funcao)
                .Include(c => c.Departamento)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(c => c.Nome.Contains(searchString) || c.CartaoPonto.Contains(searchString));
            if (departamentoId.HasValue)
                query = query.Where(c => c.DepartamentoId == departamentoId.Value);
            if (funcaoId.HasValue)
                query = query.Where(c => c.FuncaoId == funcaoId.Value);

            query = sortOrder switch
            {
                "nome_desc" => query.OrderByDescending(c => c.Nome),
                _ => query.OrderBy(c => c.Nome),
            };

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
                DepartamentoId = c.DepartamentoId,
                // --- RETORNANDO AS PERMISSÕES ---
                AcessoCafeDaManha = c.AcessoCafeDaManha,
                AcessoAlmoco = c.AcessoAlmoco,
                AcessoJanta = c.AcessoJanta,
                AcessoCeia = c.AcessoCeia
            }).ToListAsync();
        }

        public async Task<ColaboradorResponseDto> GetColaboradorByIdAsync(int id)
        {
            var colaborador = await _context.Colaboradores
                .AsNoTracking()
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
                DepartamentoId = colaborador.DepartamentoId,
                // --- RETORNANDO AS PERMISSÕES ---
                AcessoCafeDaManha = colaborador.AcessoCafeDaManha,
                AcessoAlmoco = colaborador.AcessoAlmoco,
                AcessoJanta = colaborador.AcessoJanta,
                AcessoCeia = colaborador.AcessoCeia
            };
        }
    }
}