using ApiRefeicoes.Dtos;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ApiRefeicoes.Services
{
    public interface IColaboradorService
    {
        Task<ColaboradorResponseDto> CreateColaboradorAsync(CreateColaboradorDto colaboradorDto, byte[] imagemBytes);

        // --- CORREÇÃO APLICADA AQUI ---
        // Adiciona os parâmetros de filtro e ordenação à assinatura do método.
        Task<IEnumerable<ColaboradorResponseDto>> GetAllColaboradoresAsync(string? searchString, int? departamentoId, int? funcaoId, string? sortOrder);

        Task<ColaboradorResponseDto> GetColaboradorByIdAsync(int id);
        Task<ColaboradorResponseDto> UpdateColaboradorAsync(int id, UpdateColaboradorDto colaboradorDto, Stream imagemStream);
    }
}