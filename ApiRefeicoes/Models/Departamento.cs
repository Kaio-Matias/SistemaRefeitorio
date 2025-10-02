using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Models
{
    public class Departamento
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        // Mantido conforme solicitado
        [StringLength(100)]
        public string? DepartamentoGenerico { get; set; }

        // Propriedade de navegação para EF Core
        public virtual ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
    }
}