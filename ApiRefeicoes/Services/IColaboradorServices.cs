using ApiRefeicoes.Dtos;
using ApiRefeicoes.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ApiRefeicoes.Services
{
    public interface IColaboradorService
    {
        // Assinatura corrigida para usar DTOs
        Task<ColaboradorResponseDto> CreateColaboradorAsync(CreateColaboradorDto colaboradorDto, Stream imagemStream);
        Task<IEnumerable<ColaboradorResponseDto>> GetAllColaboradoresAsync();
        Task<ColaboradorResponseDto?> GetColaboradorByIdAsync(int id);
    }
}