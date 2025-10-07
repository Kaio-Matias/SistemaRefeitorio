using System;

namespace ApiRefeicoes.Dtos
{
    public class RegistroRecenteDto
    {
        public string ColaboradorNome { get; set; }
        public string DepartamentoNome { get; set; }
        public DateTime DataHoraRegistro { get; set; }
    }
}