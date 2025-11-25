using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Dtos
{
    public class CreateJustificativaDto
    {
        [Required]
        public int RegistroRefeicaoId { get; set; }

        [Required(ErrorMessage = "O motivo é obrigatório.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "O motivo deve ter entre 5 e 500 caracteres.")]
        public string? Motivo { get; set; }

        [Required]
        public string? Responsavel { get; set; }
    }
}