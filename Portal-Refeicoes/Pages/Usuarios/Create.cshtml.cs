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
            [Required(ErrorMessage = "O nome de usu�rio � obrigat�rio.")]
            [Display(Name = "Nome de Usu�rio")]
            public string Username { get; set; }

            [Required(ErrorMessage = "O email � obrigat�rio.")]
            [EmailAddress]
            public string Email { get; set; }

            [Required(ErrorMessage = "A senha � obrigat�ria.")]
            [StringLength(100, ErrorMessage = "A {0} deve ter no m�nimo {2} e no m�ximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Senha")]
            public string Password { get; set; }

            [Required(ErrorMessage = "O perfil � obrigat�rio.")]
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

            // A l�gica para chamar a API para criar o usu�rio seria aqui.
            // Exemplo (descomente e adapte se tiver o m�todo no ApiClient):
            /*
            var success = await _apiClient.CreateUsuarioAsync(Input);
            if (success)
            {
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "N�o foi poss�vel criar o usu�rio. Tente novamente.");
                return Page();
            }
            */

            // Linha tempor�ria at� implementar a chamada da API
            // return RedirectToPage("./Index");

            // Se voc� ainda n�o tem um m�todo para criar usu�rio no ApiClient, 
            // a valida��o ocorrer� mas nada ser� salvo.
            // O c�digo do seu CreateModel anterior que usava MultipartFormDataContent tamb�m funcionaria.
            ModelState.AddModelError(string.Empty, "M�todo de cria��o na API ainda n�o implementado no ApiClient.");
            return Page();
        }
    }
}