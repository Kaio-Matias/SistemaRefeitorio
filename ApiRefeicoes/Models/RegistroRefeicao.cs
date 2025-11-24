using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiRefeicoes.Models
{
    public class RegistroRefeicao
    {
        public int Id { get; set; }

        public int ColaboradorId { get; set; }
        public virtual Colaborador Colaborador { get; set; }

        public DateTime DataHoraRegistro { get; set; }

        [StringLength(50)]
        public string TipoRefeicao { get; set; }

        [StringLength(100)]
        public string NomeColaborador { get; set; }

        [StringLength(100)]
        public string NomeDepartamento { get; set; }

        [StringLength(100)]
        public string? DepartamentoGenerico { get; set; }

        [StringLength(100)]
        public string NomeFuncao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorRefeicao { get; set; }

        public bool ParadaDeFabrica { get; set; }

        // NOVO CAMPO: Indica que o limite de 1 refeição foi excedido. 
        // O Portal deve ler este campo para exibir o "Alerta".
        public bool RefeicaoExcedente { get; set; } = false;
    }
}