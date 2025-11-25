using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ApiRefeicoes.Models
{
    public class Justificativa
    {
        [Key]
        public int Id { get; set; }

        // Vincula a justificativa a um registro de refeição específico
        public int RegistroRefeicaoId { get; set; }

        [JsonIgnore] // Evita ciclo no JSON
        public virtual RegistroRefeicao? RegistroRefeicao { get; set; }

        [Required]
        [StringLength(500)]
        public string? Motivo { get; set; }

        [StringLength(100)]
        public string? Responsavel { get; set; } // Nome do usuário que justificou (do Portal)

        public DateTime DataJustificativa { get; set; } = DateTime.UtcNow;
    }
}