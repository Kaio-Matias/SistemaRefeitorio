using System.ComponentModel.DataAnnotations;
namespace ApiRefeicoes.Dtos
{
    public class CreateUsuarioDto
    {
        [Required]
        public string? Username { get; set; }
        [Required]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string? Password { get; set; }
        [Required]
        public string? Role { get; set; }
        public bool AcessoCafeDaManha { get; set; } = false;
        public bool AcessoAlmoco { get; set; } = false;
        public bool AcessoJanta { get; set; } = false;
        public bool AcessoCeia { get; set; } = false;

    }
}
