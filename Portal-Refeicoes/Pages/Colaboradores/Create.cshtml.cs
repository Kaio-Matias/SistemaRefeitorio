using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Portal_Refeicoes.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Portal_Refeicoes.Pages.Colaboradores
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public CreateModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public ColaboradorViewModel ColaboradorInput { get; set; }

        // Propriedades para carregar os dropdowns
        public SelectList Departamentos { get; set; }
        public SelectList Funcoes { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdowns();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return Page();
            }

            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(ColaboradorInput.Nome), "Nome");
            content.Add(new StringContent(ColaboradorInput.CartaoPonto), "CartaoPonto");
            content.Add(new StringContent(ColaboradorInput.DepartamentoId.ToString()), "DepartamentoId");
            content.Add(new StringContent(ColaboradorInput.FuncaoId.ToString()), "FuncaoId");

            if (ColaboradorInput.FotoFile != null)
            {
                var streamContent = new StreamContent(ColaboradorInput.FotoFile.OpenReadStream());
                content.Add(streamContent, "FotoFile", ColaboradorInput.FotoFile.FileName);
            }

            var response = await client.PostAsync("/api/colaboradores", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Erro da API: {errorContent}");
                await LoadDropdowns();
                return Page();
            }
        }

        private async Task LoadDropdowns()
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Carregar Departamentos
            var deptResponse = await client.GetAsync("/api/departamentos");
            if (deptResponse.IsSuccessStatusCode)
            {
                var deptStream = await deptResponse.Content.ReadAsStreamAsync();
                var deptList = await JsonSerializer.DeserializeAsync<List<Departamento>>(deptStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Departamentos = new SelectList(deptList, "Id", "Nome");
            }

            // Carregar Funções
            var funcResponse = await client.GetAsync("/api/funcoes");
            if (funcResponse.IsSuccessStatusCode)
            {
                var funcStream = await funcResponse.Content.ReadAsStreamAsync();
                var funcList = await JsonSerializer.DeserializeAsync<List<Funcao>>(funcStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Funcoes = new SelectList(funcList, "Id", "Nome");
            }
        }
    }

    // ViewModel para o formulário, para lidar com o IFormFile
    public class ColaboradorViewModel
    {
        [Required]
        public string Nome { get; set; }
        [Required]
        [Display(Name = "Cartão de Ponto")]
        public string CartaoPonto { get; set; }
        [Required]
        [Display(Name = "Departamento")]
        public int DepartamentoId { get; set; }
        [Required]
        [Display(Name = "Função")]
        public int FuncaoId { get; set; }
        [Display(Name = "Foto do Colaborador")]
        public IFormFile? FotoFile { get; set; }
    }
}