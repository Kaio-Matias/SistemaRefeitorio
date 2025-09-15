using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Portal_Refeicoes.Pages.Departamentos
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public DeleteModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public Departamento Departamento { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/departamentos/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var stream = await response.Content.ReadAsStreamAsync();
            Departamento = await JsonSerializer.DeserializeAsync<Departamento>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"/api/departamentos/{Departamento.Id}");

            if (response.IsSuccessStatusCode) return RedirectToPage("./Index");

            // Adicionar tratamento de erro aqui se necessário
            return Page();
        }
    }
}