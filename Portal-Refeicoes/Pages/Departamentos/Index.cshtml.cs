using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;
using System.Text.Json;

namespace Portal_Refeicoes.Pages.Departamentos
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IList<Departamento> Departamentos { get; set; } = new List<Departamento>();

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync("/api/departamentos");

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Departamentos = await JsonSerializer.DeserializeAsync<List<Departamento>>(stream, options);
            }
        }
    }
}