using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Portal_Refeicoes.Models
{
    // Modelo para exibir a lista
    public class ColaboradorViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        [Display(Name = "Cartão Ponto")]
        public string CartaoPonto { get; set; }
        public string Funcao { get; set; }
        public string Departamento { get; set; }
        public bool Ativo { get; set; }
        public byte[]? Foto { get; set; }
        public int FuncaoId { get; set; } // Adicionado para facilitar o mapeamento
        public int DepartamentoId { get; set; } // Adicionado para facilitar o mapeamento
    }

    // Modelo para o formulário de criação
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

    // --- NOVO MODELO PARA EDIÇÃO ---
    public class ColaboradorEditModel
    {
        public int Id { get; set; }

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

        public bool Ativo { get; set; }

        // Usado para exibir a foto atual na tela
        public byte[]? FotoAtual { get; set; }
    }
}