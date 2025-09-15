using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;

namespace Portal_Refeicoes.Pages.Departamentos
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public CreateModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public Departamento Departamento { get; set; }

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
            var response = await client.PostAsJsonAsync("/api/departamentos", Departamento);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }

            // Adicionar tratamento de erro se necessário
            return Page();
        }
    }
}