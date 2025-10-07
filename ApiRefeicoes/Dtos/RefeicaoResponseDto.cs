using System;

namespace ApiRefeicoes.Dtos
{
    // DTO para representar um Colaborador de forma simples
    public class ColaboradorRefeicaoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string FuncaoNome { get; set; }
        public string DepartamentoNome { get; set; }
    }

    // DTO principal para a resposta da refeição
    public class RefeicaoResponseDto
    {
        public int Id { get; set; }
        public DateTime DataHoraRegistro { get; set; }
        public ColaboradorRefeicaoDto Colaborador { get; set; }
    }
}