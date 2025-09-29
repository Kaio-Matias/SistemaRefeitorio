using System;
using System.ComponentModel.DataAnnotations;

namespace Portal_Refeicoes.Models
{
    public class ParadaDeFabrica
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }

        public string? Descricao { get; set; }
    }
}