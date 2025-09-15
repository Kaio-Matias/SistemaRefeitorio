using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;
using System.Text.Json;

namespace Portal_Refeicoes.Pages.Colaboradores
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IList<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/colaboradores");

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Colaboradores = await JsonSerializer.DeserializeAsync<List<Colaborador>>(stream, options);
            }
        }
    }
}