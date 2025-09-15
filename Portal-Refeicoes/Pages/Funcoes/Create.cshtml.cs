using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;

namespace Portal_Refeicoes.Pages.Funcoes
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public CreateModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public Funcao Funcao { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _clientFactory.CreateClient("ApiClient");
            // Usamos o token salvo no cookie para autenticar a chamada
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("/api/funcoes", Funcao);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }

            // Adicionar tratamento de erro se a API falhar
            ModelState.AddModelError(string.Empty, "Erro ao criar a função.");
            return Page();
        }
    }
}