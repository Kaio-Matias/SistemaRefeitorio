using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;
using Portal_Refeicoes.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages.Colaboradores
{
    public class IndexModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public IndexModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // CORREÇÃO: A propriedade deve ser do tipo ColaboradorViewModel
        public IList<ColaboradorViewModel> Colaborador { get; set; }

        public async Task OnGetAsync()
        {
            // CORREÇÃO: Usa o ApiClient para buscar os dados corretamente
            Colaborador = await _apiClient.GetColaboradoresAsync();
        }
    }
}