using System.Collections.Generic; // Adicione este using
using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Models
{
    public class Departamento
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Nome { get; set; }

        public string? DepartamentoGenerico { get; set; }

        // --- INÍCIO DA CORREÇÃO (CS1061) ---
        // Propriedade de navegação para EF Core entender o relacionamento
        public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
        // --- FIM DA CORREÇÃO ---
    }
}