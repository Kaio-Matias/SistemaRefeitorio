using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Portal_Refeicoes.Models;
using System.IO;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Portal_Refeicoes.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private void SetAuthorizationHeader(HttpRequestHeaders headers)
        {
            var token = _httpContextAccessor.HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // --- MÉTODOS DE AUTENTICAÇÃO E COLABORADOR ---
        public async Task<string> LoginAsync(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { username, password });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("token").GetString();
            }
            return null;
        }

        public async Task<List<ColaboradorViewModel>> GetColaboradoresAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/colaboradores");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ColaboradorViewModel>>() : new List<ColaboradorViewModel>();
        }

        // --- MÉTODO ADICIONADO (CS1061) ---
        public async Task<ColaboradorViewModel> GetColaboradorByIdAsync(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/colaboradores/{id}");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ColaboradorViewModel>() : null;
        }

        public async Task<bool> CreateColaboradorAsync(ColaboradorCreateModel colaborador, IFormFile imagem)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(colaborador.Nome), "Nome");
            content.Add(new StringContent(colaborador.CartaoPonto), "CartaoPonto");
            content.Add(new StringContent(colaborador.FuncaoId.ToString()), "FuncaoId");
            content.Add(new StringContent(colaborador.DepartamentoId.ToString()), "DepartamentoId");
            if (imagem != null)
                content.Add(new StreamContent(imagem.OpenReadStream()), "imagem", imagem.FileName);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/colaboradores") { Content = content };
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // --- MÉTODO ADICIONADO (CS1061) ---
        public async Task<bool> UpdateColaboradorAsync(int id, ColaboradorEditModel colaborador, IFormFile? imagem)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(colaborador.Nome), "Nome");
            content.Add(new StringContent(colaborador.CartaoPonto), "CartaoPonto");
            content.Add(new StringContent(colaborador.FuncaoId.ToString()), "FuncaoId");
            content.Add(new StringContent(colaborador.DepartamentoId.ToString()), "DepartamentoId");
            content.Add(new StringContent(colaborador.Ativo.ToString().ToLower()), "Ativo");
            if (imagem != null)
                content.Add(new StreamContent(imagem.OpenReadStream()), "imagem", imagem.FileName);

            var request = new HttpRequestMessage(HttpMethod.Put, $"api/colaboradores/{id}") { Content = content };
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // --- MÉTODOS DE DEPARTAMENTO E FUNÇÃO ---
        // --- MÉTODO ADICIONADO (CS1061) ---
        public async Task<List<Departamento>> GetDepartamentosAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/departamentos");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<Departamento>>() : new List<Departamento>();
        }

        public async Task<List<Funcao>> GetFuncoesAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/funcoes");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<Funcao>>() : new List<Funcao>();
        }

        // --- MÉTODO DE SUPER ADMIN ---
        // --- MÉTODO ADICIONADO (CS1061) ---
        public async Task<bool> CreateSuperAdminAsync(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/usuarios", new { username, password, role = "SuperAdmin" });
            return response.IsSuccessStatusCode;
        }

        // --- MÉTODOS DE PARADA DE FÁBRICA ---
        public async Task<List<ParadaDeFabrica>> GetParadasDeFabricaAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/ParadaDeFabrica");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<List<ParadaDeFabrica>>() : new List<ParadaDeFabrica>();
        }

        public async Task<bool> CreateParadaDeFabricaAsync(ParadaDeFabrica parada)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/ParadaDeFabrica") { Content = JsonContent.Create(parada) };
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteParadaDeFabricaAsync(int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/ParadaDeFabrica/{id}");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}