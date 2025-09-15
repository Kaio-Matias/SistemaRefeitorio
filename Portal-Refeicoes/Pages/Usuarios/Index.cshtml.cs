using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
// Importe o namespace que contém sua classe de modelo
using Portal_Refeicoes.Models;
using System.Net.Http.Headers;
using System.Text.Json;

// O namespace aqui deve refletir o novo nome da pasta
namespace Portal_Refeicoes.Pages.Usuarios
{
    [Authorize(Roles = "SuperAdmin")]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        public IndexModel(IHttpClientFactory factory) { _clientFactory = factory; }

        // A lista agora usa o nome completo da classe para evitar qualquer ambiguidade
        public List<Portal_Refeicoes.Models.Usuario> Usuarios { get; set; } = new();

        public async Task OnGetAsync()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("/api/usuarios");
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                // Aqui também, usamos o nome completo da classe
                Usuarios = await JsonSerializer.DeserializeAsync<List<Portal_Refeicoes.Models.Usuario>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
    }
}