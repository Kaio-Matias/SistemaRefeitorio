using ApiRefeicoes.Data;
using ApiRefeicoes.Dtos;
using ApiRefeicoes.Models;
using Microsoft.EntityFrameworkCore;
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

        public ColaboradorService(ApiRefeicoesDbContext context, FaceApiService faceApiService)
        {
            _context = context;
            _faceApiService = faceApiService;
        }

        public async Task<ColaboradorResponseDto> CreateColaboradorAsync(CreateColaboradorDto colaboradorDto, Stream imagemStream)
        {
            // Mapeia do DTO para o Modelo
            var colaborador = new Colaborador
            {
                Nome = colaboradorDto.Nome,
                CartaoPonto = colaboradorDto.CartaoPonto,
                FuncaoId = colaboradorDto.FuncaoId,
                DepartamentoId = colaboradorDto.DepartamentoId,
                Ativo = true // Define como ativo por padrão
            };

            using (var memoryStream = new MemoryStream())
            {
                await imagemStream.CopyToAsync(memoryStream);
                colaborador.Foto = memoryStream.ToArray(); // Salva a foto no banco
            }

            _context.Colaboradores.Add(colaborador);
            await _context.SaveChangesAsync();

            // Cadastra a face na API do Azure
            imagemStream.Position = 0; // Reseta o stream para o Face API usar
            var personId = await _faceApiService.CreatePersonAsync(colaborador.Nome);
            await _faceApiService.AddFaceToPersonAsync(personId, imagemStream);

            // Inicia o treinamento do grupo de pessoas
            await _faceApiService.TrainPersonGroupAsync();

            // Atribuição direta do Guid, corrigindo o erro de conversão
            colaborador.PersonId = personId;
            await _context.SaveChangesAsync();

            // Mapeia do Modelo para o DTO de resposta
            var funcao = await _context.Funcoes.FindAsync(colaborador.FuncaoId);
            var departamento = await _context.Departamentos.FindAsync(colaborador.DepartamentoId);

            var responseDto = new ColaboradorResponseDto
            {
                Id = colaborador.Id,
                Nome = colaborador.Nome,
                CartaoPonto = colaborador.CartaoPonto,
                Funcao = funcao?.Nome ?? "N/A",
                Departamento = departamento?.Nome ?? "N/A",
                Ativo = colaborador.Ativo
            };

            return responseDto;
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
                    Funcao = c.Funcao.Nome,
                    Departamento = c.Departamento.Nome,
                    Ativo = c.Ativo
                }).ToListAsync();
        }

        public async Task<ColaboradorResponseDto?> GetColaboradorByIdAsync(int id)
        {
            var colaborador = await _context.Colaboradores
                .Include(c => c.Funcao)
                .Include(c => c.Departamento)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (colaborador == null)
            {
                return null;
            }

            return new ColaboradorResponseDto
            {
                Id = colaborador.Id,
                Nome = colaborador.Nome,
                CartaoPonto = colaborador.CartaoPonto,
                Funcao = colaborador.Funcao.Nome,
                Departamento = colaborador.Departamento.Nome,
                Ativo = colaborador.Ativo
            };
        }
    }
}