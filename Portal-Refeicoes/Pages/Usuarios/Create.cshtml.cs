using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;

namespace Portal_Refeicoes.Pages.Usuarios
{
    [Authorize(Roles = "SuperAdmin")] // Apenas SuperAdmin pode criar usuários
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        public CreateModel(IHttpClientFactory factory) { _clientFactory = factory; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Nome { get; set; }
            [Required, EmailAddress]
            public string Email { get; set; }
            [Required, DataType(DataType.Password)]
            public string Senha { get; set; }
            [Required]
            public string Role { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(Input.Nome), "Nome");
            content.Add(new StringContent(Input.Email), "Email");
            content.Add(new StringContent(Input.Senha), "Senha");
            content.Add(new StringContent(Input.Role), "Role");

            var response = await client.PostAsync("/api/usuarios", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Erro ao criar usuário.");
                return Page();
            }
        }
    }
}