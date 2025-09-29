using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Portal_Refeicoes.Models;
using Portal_Refeicoes.Services;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages.Colaboradores
{
    public class CreateModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public CreateModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [BindProperty]
        public ColaboradorCreateModel Colaborador { get; set; }

        [BindProperty]
        public IFormFile Imagem { get; set; }

        public SelectList Departamentos { get; set; }
        public SelectList Funcoes { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // --- Validação do tipo de arquivo ---
            if (Imagem != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(Imagem.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Imagem", "Por favor, envie um arquivo de imagem válido (JPG, PNG, GIF).");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return Page();
            }

            var success = await _apiClient.CreateColaboradorAsync(Colaborador, Imagem);

            if (success)
            {
                return RedirectToPage("./Index");
            }

            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar o colaborador. Verifique os dados e tente novamente.");
            await PopulateDropdowns();
            return Page();
        }

        private async Task PopulateDropdowns()
        {
            var departamentos = await _apiClient.GetDepartamentosAsync();
            var funcoes = await _apiClient.GetFuncoesAsync();
            Departamentos = new SelectList(departamentos, "Id", "Nome");
            Funcoes = new SelectList(funcoes, "Id", "Nome");
        }
    }
}