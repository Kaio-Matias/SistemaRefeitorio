using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portal_Refeicoes.Models;
using Portal_Refeicoes.Pages.Usuarios; // Adicionado para simplificar a referência
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

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

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.User?.FindFirst("access_token")?.Value;

            _httpClient.DefaultRequestHeaders.Authorization = null;

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // --- MÉTODOS DE USUÁRIO E REFEIÇÃO ---

        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            AddAuthorizationHeader();
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Usuario>>("api/Usuarios");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuários da API.");
                return new List<Usuario>();
            }
        }

        // --- CORREÇÃO APLICADA AQUI ---
        // Usamos o nome completo da classe interna 'InputModel' que está dentro de 'CreateModel'
        public async Task<bool> CreateUsuarioAsync(CreateModel.InputModel usuario)
        {
            AddAuthorizationHeader();
            // A API espera um objeto com as propriedades Username, Password e Role.
            var response = await _httpClient.PostAsJsonAsync("api/Usuarios", new
            {
                Username = usuario.Username,
                Password = usuario.Password,
                Email = usuario.Email,
                Role = usuario.Role
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao criar usuário: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return false;
            }
            return true;
        }

        public async Task<List<RefeicaoViewModel>> GetRefeicoesAsync()
        {
            AddAuthorizationHeader();
            try
            {
                return await _httpClient.GetFromJsonAsync<List<RefeicaoViewModel>>("api/Refeicoes");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro ao buscar refeições da API.");
                return new List<RefeicaoViewModel>();
            }
        }

        // --- MÉTODOS DE COLABORADOR ---

        public async Task<List<ColaboradorViewModel>> GetColaboradoresAsync()
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<List<ColaboradorViewModel>>("api/colaboradores") ?? new List<ColaboradorViewModel>();
        }

        public async Task<ColaboradorViewModel> GetColaboradorByIdAsync(int id)
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<ColaboradorViewModel>($"api/colaboradores/{id}");
        }

        public async Task<bool> CreateColaboradorAsync(ColaboradorCreateModel colaborador, IFormFile imagem)
        {
            AddAuthorizationHeader();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(colaborador.Nome), "Nome");
            content.Add(new StringContent(colaborador.CartaoPonto), "CartaoPonto");
            content.Add(new StringContent(colaborador.FuncaoId.ToString()), "FuncaoId");
            content.Add(new StringContent(colaborador.DepartamentoId.ToString()), "DepartamentoId");
            if (imagem != null)
                content.Add(new StreamContent(imagem.OpenReadStream()), "imagem", imagem.FileName);

            var response = await _httpClient.PostAsync("api/colaboradores", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateColaboradorAsync(int id, ColaboradorEditModel colaborador, IFormFile? imagem)
        {
            AddAuthorizationHeader();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(colaborador.Nome), "Nome");
            content.Add(new StringContent(colaborador.CartaoPonto), "CartaoPonto");
            content.Add(new StringContent(colaborador.FuncaoId.ToString()), "FuncaoId");
            content.Add(new StringContent(colaborador.DepartamentoId.ToString()), "DepartamentoId");
            content.Add(new StringContent(colaborador.Ativo.ToString().ToLower()), "Ativo");
            if (imagem != null)
                content.Add(new StreamContent(imagem.OpenReadStream()), "imagem", imagem.FileName);

            var response = await _httpClient.PutAsync($"api/colaboradores/{id}", content);
            return response.IsSuccessStatusCode;
        }

        // --- MÉTODOS DE DEPARTAMENTO E FUNÇÃO ---

        public async Task<List<Departamento>> GetDepartamentosAsync()
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<List<Departamento>>("api/departamentos") ?? new List<Departamento>();
        }

        public async Task<List<Funcao>> GetFuncoesAsync()
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<List<Funcao>>("api/funcoes") ?? new List<Funcao>();
        }

        // --- MÉTODOS DE PARADA DE FÁBRICA ---

        public async Task<List<ParadaDeFabrica>> GetParadasDeFabricaAsync()
        {
            AddAuthorizationHeader();
            return await _httpClient.GetFromJsonAsync<List<ParadaDeFabrica>>("api/ParadaDeFabrica") ?? new List<ParadaDeFabrica>();
        }

        public async Task<bool> CreateParadaDeFabricaAsync(ParadaDeFabrica parada)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("api/ParadaDeFabrica", parada);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteParadaDeFabricaAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"api/ParadaDeFabrica/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}