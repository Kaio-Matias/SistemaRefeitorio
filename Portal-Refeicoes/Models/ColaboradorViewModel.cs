using System.ComponentModel.DataAnnotations;

namespace Portal_Refeicoes.Models
{
    // Usado para exibir a lista de colaboradores
    public class ColaboradorViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        [Display(Name = "Cartão Ponto")]
        public string CartaoPonto { get; set; }
        [Display(Name = "Função")]
        public string Funcao { get; set; }
        public string Departamento { get; set; }
        public bool Ativo { get; set; }
    }

    // Usado no formulário de criação
    public class ColaboradorCreateModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O cartão de ponto é obrigatório.")]
        [Display(Name = "Cartão Ponto")]
        public string CartaoPonto { get; set; }

        [Required(ErrorMessage = "A função é obrigatória.")]
        [Display(Name = "Função")]
        public int FuncaoId { get; set; }

        [Required(ErrorMessage = "O departamento é obrigatório.")]
        [Display(Name = "Departamento")]
        public int DepartamentoId { get; set; }
    }
}