using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;
using System.Text.Json;

namespace Portal_Refeicoes.Pages.Funcoes
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IList<Funcao> Funcoes { get; set; } = new List<Funcao>();

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync("/api/funcoes");

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Funcoes = await JsonSerializer.DeserializeAsync<List<Funcao>>(stream, options);
            }
        }
    }
}