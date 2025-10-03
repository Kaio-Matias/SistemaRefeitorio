using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Models;
using Portal_Refeicoes.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages.Refeicoes
{
    public class IndexModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public IndexModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IList<RefeicaoViewModel> Refeicoes { get; set; } = new List<RefeicaoViewModel>();

        public async Task OnGetAsync()
        {
            Refeicoes = await _apiClient.GetRefeicoesAsync();
        }
    }
}