using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Portal_Refeicoes.Models;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Portal_Refeicoes.Pages.Colaboradores
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public EditModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // Usaremos um InputModel para os dados do formulário
        [BindProperty]
        public InputModel Input { get; set; }

        // Propriedade para exibir a foto existente
        public byte[]? FotoAtual { get; set; }

        public SelectList Departamentos { get; set; }
        public SelectList Funcoes { get; set; }

        // ViewModel interno para o formulário
        public class InputModel
        {
            public int Id { get; set; }
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
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/colaboradores/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var stream = await response.Content.ReadAsStreamAsync();
            var colaborador = await JsonSerializer.DeserializeAsync<Colaborador>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (colaborador == null) return NotFound();

            // Popula o InputModel com os dados do colaborador
            Input = new InputModel
            {
                Id = colaborador.Id,
                Nome = colaborador.Nome,
                CartaoPonto = colaborador.CartaoPonto,
                DepartamentoId = colaborador.DepartamentoId,
                FuncaoId = colaborador.FuncaoId
            };

            FotoAtual = colaborador.Foto;

            await LoadDropdowns(colaborador.DepartamentoId, colaborador.FuncaoId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(Input.DepartamentoId, Input.FuncaoId);
                return Page();
            }

            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // ETAPA 1: Atualizar os dados do colaborador (Nome, Cartão de Ponto, etc.)
            var dataResponse = await client.PutAsJsonAsync($"/api/colaboradores/{Input.Id}", Input);

            if (!dataResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Erro ao atualizar os dados do colaborador.");
                await LoadDropdowns(Input.DepartamentoId, Input.FuncaoId);
                return Page();
            }

            // ETAPA 2: Verificar se um ficheiro de FOTO foi enviado e atualizá-lo
            var fotoFile = Request.Form.Files.GetFile("NovaFoto");
            if (fotoFile != null && fotoFile.Length > 0)
            {
                using var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(fotoFile.OpenReadStream());
                content.Add(streamContent, "fotoFile", fotoFile.FileName);

                var fotoResponse = await client.PutAsync($"/api/colaboradores/{Input.Id}/foto", content);
                if (!fotoResponse.IsSuccessStatusCode)
                {
                    TempData["WarningMessage"] = "Dados do colaborador atualizados, mas falha ao enviar a nova foto.";
                    return RedirectToPage("./Index");
                }
            }

            TempData["SuccessMessage"] = "Colaborador atualizado com sucesso!";
            return RedirectToPage("./Index");
        }

        private async Task LoadDropdowns(int? selectedDeptId = null, int? selectedFuncId = null)
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var token = User.FindFirst("access_token")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Carregar Departamentos
            var deptResponse = await client.GetAsync("/api/departamentos");
            if (deptResponse.IsSuccessStatusCode)
            {
                var deptStream = await deptResponse.Content.ReadAsStreamAsync();
                var deptList = await JsonSerializer.DeserializeAsync<List<Departamento>>(deptStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Departamentos = new SelectList(deptList, "Id", "Nome", selectedDeptId);
            }

            // Carregar Funções
            var funcResponse = await client.GetAsync("/api/funcoes");
            if (funcResponse.IsSuccessStatusCode)
            {
                var funcStream = await funcResponse.Content.ReadAsStreamAsync();
                var funcList = await JsonSerializer.DeserializeAsync<List<Funcao>>(funcStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Funcoes = new SelectList(funcList, "Id", "Nome", selectedFuncId);
            }
        }
    }
}