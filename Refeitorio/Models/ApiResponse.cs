using System.Text.Json.Serialization;

namespace Refeitorio.Models
{
    // Esta classe irá corresponder à resposta JSON da sua API
    public class ApiResponse
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("colaboradorNome")]
        public string ColaboradorNome { get; set; }
    }
}