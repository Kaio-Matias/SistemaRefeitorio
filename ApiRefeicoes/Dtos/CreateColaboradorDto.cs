using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Dtos
{
    public class CreateColaboradorDto
    {
        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required]
        [StringLength(20)]
        public string CartaoPonto { get; set; }

        [Required]
        public int FuncaoId { get; set; }

        [Required]
        public int DepartamentoId { get; set; }
        public bool AcessoCafeDaManha { get; set; } = false;
        public bool AcessoAlmoco { get; set; } = false;
        public bool AcessoJanta { get; set; } = false;
        public bool AcessoCeia { get; set; } = false;
    }
}