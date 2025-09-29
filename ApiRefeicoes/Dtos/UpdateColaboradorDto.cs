using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Dtos
{
    public class UpdateColaboradorDto
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

        public bool Ativo { get; set; }
    }
}