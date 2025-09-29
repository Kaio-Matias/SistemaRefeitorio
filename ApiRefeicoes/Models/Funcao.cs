using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Models
{
    public class Funcao
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        // --- INÍCIO DA CORREÇÃO (CS1061) ---
        // Adiciona a propriedade de navegação para a coleção de Colaboradores
        public virtual ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
        // --- FIM DA CORREÇÃO ---
    }
}