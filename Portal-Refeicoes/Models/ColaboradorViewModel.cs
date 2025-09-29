using System.ComponentModel.DataAnnotations;

namespace Portal_Refeicoes.Models
{
    // Usado para EXIBIR a lista de colaboradores
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

        // --- INÍCIO DA CORREÇÃO ---
        // Adiciona a propriedade da foto aqui
        public byte[]? Foto { get; set; }
        // --- FIM DA CORREÇÃO ---
    }

    // Usado no formulário de CRIAÇÃO
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

        // --- CORREÇÃO ---
        // A foto no formulário de criação é tratada pelo IFormFile, 
        // então não precisamos dela aqui.
        // public byte[]? Foto { get; set; } // << REMOVER ESTA LINHA
    }
}