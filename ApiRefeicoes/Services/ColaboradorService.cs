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
            var colaborador = new Colaborador
            {
                Nome = colaboradorDto.Nome,
                CartaoPonto = colaboradorDto.CartaoPonto,
                FuncaoId = colaboradorDto.FuncaoId,
                DepartamentoId = colaboradorDto.DepartamentoId,
                Ativo = true
            };

            using (var memoryStream = new MemoryStream())
            {
                await imagemStream.CopyToAsync(memoryStream);
                colaborador.Foto = memoryStream.ToArray();
                memoryStream.Position = 0;

                var personId = await _faceApiService.CreatePersonAsync(colaborador.Nome);
                await _faceApiService.AddFaceToPersonAsync(personId, memoryStream);
                colaborador.PersonId = personId;
            }

            _context.Colaboradores.Add(colaborador);
            await _context.SaveChangesAsync();
            await _faceApiService.TrainPersonGroupAsync();

            return await GetColaboradorByIdAsync(colaborador.Id);
        }

        public async Task<ColaboradorResponseDto?> UpdateColaboradorAsync(int id, UpdateColaboradorDto colaboradorDto, Stream? imagemStream)
        {
            var colaborador = await _context.Colaboradores.FindAsync(id);
            if (colaborador == null) return null;

            if (imagemStream != null)
            {
                if (colaborador.PersonId.HasValue)
                {
                    await _faceApiService.DeletePersonAsync(colaborador.PersonId.Value);
                }

                using (var memoryStream = new MemoryStream())
                {
                    await imagemStream.CopyToAsync(memoryStream);
                    colaborador.Foto = memoryStream.ToArray();
                    memoryStream.Position = 0;

                    var novoPersonId = await _faceApiService.CreatePersonAsync(colaboradorDto.Nome);
                    await _faceApiService.AddFaceToPersonAsync(novoPersonId, memoryStream);
                    colaborador.PersonId = novoPersonId;
                }
                await _faceApiService.TrainPersonGroupAsync();
            }

            colaborador.Nome = colaboradorDto.Nome;
            colaborador.CartaoPonto = colaboradorDto.CartaoPonto;
            colaborador.FuncaoId = colaboradorDto.FuncaoId;
            colaborador.DepartamentoId = colaboradorDto.DepartamentoId;
            colaborador.Ativo = colaboradorDto.Ativo; // Atribui para a propriedade bool, que fará a conversão para o AtivoStorage

            _context.Colaboradores.Update(colaborador);
            await _context.SaveChangesAsync();

            return await GetColaboradorByIdAsync(id);
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
                    Ativo = c.Ativo, // Usa a propriedade booleana 'Ativo'
                    Foto = c.Foto,
                    FuncaoId = c.FuncaoId,
                    DepartamentoId = c.DepartamentoId
                }).ToListAsync();
        }

        public async Task<ColaboradorResponseDto?> GetColaboradorByIdAsync(int id)
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
                Funcao = colaborador.Funcao.Nome,
                Departamento = colaborador.Departamento.Nome,
                Ativo = colaborador.Ativo, // Usa a propriedade booleana 'Ativo'
                Foto = colaborador.Foto,
                FuncaoId = colaborador.FuncaoId,
                DepartamentoId = colaborador.DepartamentoId
            };
        }
    }
}