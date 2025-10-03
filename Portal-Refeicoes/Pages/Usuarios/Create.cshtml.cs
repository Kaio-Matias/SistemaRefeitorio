using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Services; // Usando o ApiClient
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages.Usuarios
{
    [Authorize(Roles = "SuperAdmin")]
    public class CreateModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public CreateModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
            [Display(Name = "Nome de Usuário")]
            public string Username { get; set; }

            [Required(ErrorMessage = "O email é obrigatório.")]
            [EmailAddress]
            public string Email { get; set; }

            [Required(ErrorMessage = "A senha é obrigatória.")]
            [StringLength(100, ErrorMessage = "A {0} deve ter no mínimo {2} e no máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Senha")]
            public string Password { get; set; }

            [Required(ErrorMessage = "O perfil é obrigatório.")]
            public string Role { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // A lógica para chamar a API para criar o usuário seria aqui.
            // Exemplo (descomente e adapte se tiver o método no ApiClient):
            /*
            var success = await _apiClient.CreateUsuarioAsync(Input);
            if (success)
            {
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Não foi possível criar o usuário. Tente novamente.");
                return Page();
            }
            */

            // Linha temporária até implementar a chamada da API
            // return RedirectToPage("./Index");

            // Se você ainda não tem um método para criar usuário no ApiClient, 
            // a validação ocorrerá mas nada será salvo.
            // O código do seu CreateModel anterior que usava MultipartFormDataContent também funcionaria.
            ModelState.AddModelError(string.Empty, "Método de criação na API ainda não implementado no ApiClient.");
            return Page();
        }
    }
}