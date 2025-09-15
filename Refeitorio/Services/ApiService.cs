using Refeitorio.Models;
using System.Net.Http.Json;

namespace Refeitorio.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse> IdentificarColaborador(Stream fotoStream)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(new StreamContent(fotoStream), "fotoFile", "rosto.jpg");

                var response = await _httpClient.PostAsync("/api/identificacao/identificar", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    if (result != null)
                    {
                        result.IsSuccess = true;
                        return result;
                    }
                    return new ApiResponse { IsSuccess = false, Message = "Resposta inválida da API." };
                }
                else
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    return new ApiResponse { IsSuccess = false, Message = errorResult?.Message ?? "Colaborador não reconhecido." };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse { IsSuccess = false, Message = $"Erro de comunicação: {ex.Message}" };
            }
        }
    }
}