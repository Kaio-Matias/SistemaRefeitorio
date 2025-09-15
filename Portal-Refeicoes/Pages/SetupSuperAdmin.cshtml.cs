using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Portal_Refeicoes.Pages
{
    public class SetupSuperAdminModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public SetupSuperAdminModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string Nome { get; set; }
            [Required]
            [EmailAddress]
            public string Email { get; set; }
            [Required]
            [DataType(DataType.Password)]
            public string Senha { get; set; }
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

            var client = _clientFactory.CreateClient("ApiClient");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(Input.Nome), "Nome");
            content.Add(new StringContent(Input.Email), "Email");
            content.Add(new StringContent(Input.Senha), "Senha");
            content.Add(new StringContent("SuperAdmin"), "Role"); // Hardcoded para garantir que seja SuperAdmin

            var response = await client.PostAsync("/api/usuarios", content);

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "Usuário SuperAdmin criado com sucesso! Agora você pode fazer login.";
                ModelState.Clear(); // Limpa o formulário
                return Page();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Falha ao criar usuário. A API respondeu com: {response.StatusCode}. Detalhes: {errorContent}";
                return Page();
            }
        }
    }
}