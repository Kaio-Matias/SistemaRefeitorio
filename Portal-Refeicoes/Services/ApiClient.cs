using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Portal_Refeicoes.Models;
using System.IO;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging; // Adicionado para logging

namespace Portal_Refeicoes.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiClient> _logger; // Injetando o logger

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
                _logger.LogInformation("[ApiClient] Token encontrado na sessão. Adicionando ao cabeçalho da requisição.");
                // Limpa o cabeçalho antes de adicionar para evitar duplicatas
                headers.Authorization = null;
                headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("[ApiClient] Token NÃO encontrado na sessão. A requisição será enviada sem autorização.");
            }
        }

        // ... (métodos LoginAsync, GetColaboradoresAsync, etc. permanecem iguais)
        public async Task<string> LoginAsync(string username, string password)
        {
            var loginRequest = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var token = loginResponse.GetProperty("token").GetString();
                _logger.LogInformation("[ApiClient] Login bem-sucedido. Token recebido.");
                return token;
            }
            _logger.LogWarning("[ApiClient] Falha no login para o usuário {Username}", username);
            return null;
        }

        public async Task<List<ColaboradorViewModel>> GetColaboradoresAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/colaboradores");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ColaboradorViewModel>>();
            }
            return new List<ColaboradorViewModel>();
        }

        public async Task<bool> CreateColaboradorAsync(ColaboradorCreateModel colaborador, IFormFile imagem)
        {
            _logger.LogInformation("[ApiClient] Preparando para enviar requisição de criação de colaborador.");
            using var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent(colaborador.Nome), "Nome");
            multipartContent.Add(new StringContent(colaborador.CartaoPonto), "CartaoPonto");
            multipartContent.Add(new StringContent(colaborador.FuncaoId.ToString()), "FuncaoId");
            multipartContent.Add(new StringContent(colaborador.DepartamentoId.ToString()), "DepartamentoId");

            if (imagem != null && imagem.Length > 0)
            {
                var streamContent = new StreamContent(imagem.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(imagem.ContentType);
                multipartContent.Add(streamContent, "imagem", imagem.FileName);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "api/colaboradores");
            request.Content = multipartContent;

            // Log antes de setar o Header
            _logger.LogInformation("[ApiClient] Tentando adicionar cabeçalho de autorização...");
            SetAuthorizationHeader(request.Headers);

            _logger.LogInformation("[ApiClient] Enviando requisição para a API...");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("[ApiClient] A API retornou um erro: StatusCode={StatusCode}, Content={ErrorContent}", response.StatusCode, errorContent);
            }
            else
            {
                _logger.LogInformation("[ApiClient] A API retornou sucesso (StatusCode={StatusCode})", response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }

        // ... (outros métodos permanecem iguais)
        public async Task<List<Departamento>> GetDepartamentosAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/departamentos");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Departamento>>();
            }
            return new List<Departamento>();
        }

        public async Task<List<Funcao>> GetFuncoesAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/funcoes");
            SetAuthorizationHeader(request.Headers);
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Funcao>>();
            }
            return new List<Funcao>();
        }

        public async Task<bool> CreateSuperAdminAsync(string username, string password)
        {
            var createUserRequest = new
            {
                Username = username,
                Password = password,
                Role = "SuperAdmin"
            };
            var response = await _httpClient.PostAsJsonAsync("api/usuarios", createUserRequest);
            return response.IsSuccessStatusCode;
        }
    }
}