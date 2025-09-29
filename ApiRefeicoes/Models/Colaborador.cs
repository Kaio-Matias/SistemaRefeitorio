using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Models
{
    public class Colaborador
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required]
        [StringLength(20)]
        public string CartaoPonto { get; set; }

        public byte[]? Foto { get; set; }

        public int FuncaoId { get; set; }
        public virtual Funcao? Funcao { get; set; }

        public int DepartamentoId { get; set; }
        public virtual Departamento? Departamento { get; set; }

        public bool Ativo { get; set; } = true;

        public Guid? PersonId { get; set; }

        // --- GARANTIR QUE ESTA PROPRIEDADE EXISTA ---
        public virtual ICollection<RegistroRefeicao> RegistrosRefeicoes { get; set; } = new List<RegistroRefeicao>();
    }
}