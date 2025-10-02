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

        public async Task<IEnumerable<ColaboradorResponseDto>> GetAllColaboradoresAsync()
        {
            return await _context.Colaboradores
                .Include(c => c.Funcao)
                .Include(c => c.Departamento)
                .Select(c => new ColaboradorResponseDto
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
                })
                .ToListAsync();
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

        public async Task<ColaboradorResponseDto> UpdateColaboradorAsync(int id, UpdateColaboradorDto colaboradorDto, Stream imagemStream)
        {
            var colaborador = await _context.Colaboradores.FindAsync(id);
            if (colaborador == null) return null;
            colaborador.Nome = colaboradorDto.Nome;
            colaborador.CartaoPonto = colaboradorDto.CartaoPonto;
            await _context.SaveChangesAsync();

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
    }
}