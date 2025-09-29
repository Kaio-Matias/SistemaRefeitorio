using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiRefeicoes.Models
{
    public class RegistroRefeicao
    {
        public int Id { get; set; }

        // Relacionamento com Colaborador
        public int ColaboradorId { get; set; }
        public Colaborador? Colaborador { get; set; }

        // Data e Hora do Registro
        public DateTime DataHoraRegistro { get; set; }

        // Informações da Refeição
        public string? TipoRefeicao { get; set; } // Ex: "Almoço", "Janta"

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorRefeicao { get; set; }

        // Flag para casos especiais
        public bool ParadaDeFabrica { get; set; }
    }
}