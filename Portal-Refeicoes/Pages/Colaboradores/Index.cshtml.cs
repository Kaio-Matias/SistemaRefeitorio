using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;
using Portal_Refeicoes.Services; // Adicione o using para o ApiClient
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages.Colaboradores
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApiClient _apiClient;

        // Correção 1: Injetar o ApiClient diretamente, assim como nas outras páginas.
        public IndexModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Correção 2: Usar o ColaboradorViewModel, que é o tipo correto retornado pela API.
        public IList<ColaboradorViewModel> Colaboradores { get; set; } = new List<ColaboradorViewModel>();

        public async Task OnGetAsync()
        {
            // Correção 3: Chamar o método GetColaboradoresAsync do ApiClient, que já cuida de tudo.
            var colaboradores = await _apiClient.GetColaboradoresAsync();
            if (colaboradores != null)
            {
                Colaboradores = colaboradores;
            }
        }
    }
}