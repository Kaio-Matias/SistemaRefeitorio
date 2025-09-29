using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Portal_Refeicoes.Models;
using Portal_Refeicoes.Services;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages.Colaboradores
{
    public class EditModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public EditModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [BindProperty]
        public ColaboradorEditModel Colaborador { get; set; }

        [BindProperty]
        public IFormFile? NovaImagem { get; set; }

        public SelectList Departamentos { get; set; }
        public SelectList Funcoes { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var colaboradorDaApi = await _apiClient.GetColaboradorByIdAsync(id.Value);
            if (colaboradorDaApi == null) return NotFound();

            Colaborador = new ColaboradorEditModel
            {
                Id = colaboradorDaApi.Id,
                Nome = colaboradorDaApi.Nome,
                CartaoPonto = colaboradorDaApi.CartaoPonto,
                Ativo = colaboradorDaApi.Ativo,
                FotoAtual = colaboradorDaApi.Foto,
                FuncaoId = colaboradorDaApi.FuncaoId,
                DepartamentoId = colaboradorDaApi.DepartamentoId
            };

            await PopulateDropdowns(Colaborador.DepartamentoId, Colaborador.FuncaoId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(Colaborador.DepartamentoId, Colaborador.FuncaoId);
                return Page();
            }

            var success = await _apiClient.UpdateColaboradorAsync(Colaborador.Id, Colaborador, NovaImagem);

            if (success)
            {
                return RedirectToPage("./Index");
            }

            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao tentar atualizar o colaborador.");
            await PopulateDropdowns(Colaborador.DepartamentoId, Colaborador.FuncaoId);
            return Page();
        }

        private async Task PopulateDropdowns(int? deptoId, int? funcaoId)
        {
            var departamentos = await _apiClient.GetDepartamentosAsync();
            var funcoes = await _apiClient.GetFuncoesAsync();
            Departamentos = new SelectList(departamentos, "Id", "Nome", deptoId);
            Funcoes = new SelectList(funcoes, "Id", "Nome", funcaoId);
        }
    }
}