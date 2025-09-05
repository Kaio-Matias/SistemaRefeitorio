using System;

namespace ApiRefeicoes.Models
{
    public class RegistroRefeicao
    {
        public int Id { get; set; }
        public DateTime HorarioRegistro { get; set; }
        public decimal ValorRefeicao { get; set; }
        public string Dispositivo { get; set; }
        public int ColaboradorId { get; set; }
        public Colaborador Colaborador { get; set; }
    }
}