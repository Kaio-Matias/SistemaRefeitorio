using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiRefeicoes.Models
{
    public class Colaborador
    {
        [Key]
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

        public bool AcessoCafeDaManha { get; set; } = false;
        public bool AcessoAlmoco { get; set; } = false;
        public bool AcessoJanta { get; set; } = false;
        public bool AcessoCeia { get; set; } = false;

        public ICollection<RegistroRefeicao> RegistrosRefeicoes { get; set; } = new List<RegistroRefeicao>();
    }
}