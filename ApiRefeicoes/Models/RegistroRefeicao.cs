using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiRefeicoes.Models
{
    public class RegistroRefeicao
    {
        public int Id { get; set; }

        // Chave estrangeira para o Colaborador
        public int ColaboradorId { get; set; }
        // Propriedade de navegação para o Colaborador
        public virtual Colaborador Colaborador { get; set; }

        public DateTime DataHoraRegistro { get; set; }

        [StringLength(50)]
        public string TipoRefeicao { get; set; } // Ex: "Almoço", "Janta"

        // Campos para armazenar os dados no momento do registro (snapshot)
        [StringLength(100)]
        public string NomeColaborador { get; set; }

        [StringLength(100)]
        public string NomeDepartamento { get; set; }

        // Campo adicionado para o snapshot do Departamento Genérico
        [StringLength(100)]
        public string? DepartamentoGenerico { get; set; }

        [StringLength(100)]
        public string NomeFuncao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorRefeicao { get; set; }

        public bool ParadaDeFabrica { get; set; }
    }
}