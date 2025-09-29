using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// --- CORREÇÃO (CS0118): Referência explícita ao modelo para evitar conflito de nome ---
using Portal_Refeicoes.Models;
using Portal_Refeicoes.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages.ParadaDeFabrica
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public IndexModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // --- CORREÇÃO (CS0118): Usa o nome completo do tipo ---
        public IList<Models.ParadaDeFabrica> Paradas { get; set; } = new List<Models.ParadaDeFabrica>();

        [BindProperty]
        // --- CORREÇÃO (CS0118): Usa o nome completo do tipo ---
        public Models.ParadaDeFabrica NovaParada { get; set; }

        public async Task OnGetAsync()
        {
            Paradas = await _apiClient.GetParadasDeFabricaAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Paradas = await _apiClient.GetParadasDeFabricaAsync();
                return Page();
            }

            var success = await _apiClient.CreateParadaDeFabricaAsync(NovaParada);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Não foi possível agendar a parada. A data pode já existir.");
                Paradas = await _apiClient.GetParadasDeFabricaAsync();
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _apiClient.DeleteParadaDeFabricaAsync(id);
            return RedirectToPage();
        }
    }
}