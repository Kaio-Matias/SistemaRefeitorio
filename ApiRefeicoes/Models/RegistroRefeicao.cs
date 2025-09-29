using System;

namespace ApiRefeicoes.Models
{
    public class RegistroRefeicao
    {
        public int Id { get; set; }
        public int ColaboradorId { get; set; }
        public Colaborador? Colaborador { get; set; }
        public DateTime? DataRefeicao { get; set; }
        public DateTime? HorarioRegistro { get; set; }
        public DateTime? Day { get; set; }  
        public decimal ValorRefeicao { get; set; }
        public string? TipoRefeicao { get; set; } 
        public Colaborador? Nome { get; set; }   
        public string? Funcao { get; set; }
        public string? Departamento { get; set; }
        public string? CartaoPonto { get; set; }
        public string? ParadaDeFabrica { get; set; }    



    }
}