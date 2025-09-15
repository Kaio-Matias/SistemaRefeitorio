using System;
using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Models
{
    public class Cardapio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string NomeArquivo { get; set; }

        [Required]
        public string CaminhoArquivo { get; set; }

        public DateTime DataUpload { get; set; } = DateTime.UtcNow;
    }
}