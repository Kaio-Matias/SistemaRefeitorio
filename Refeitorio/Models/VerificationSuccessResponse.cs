using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Refeitorio.Models
{
    public class VerificationSuccessResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonPropertyName("colaborador")]
        public string Colaborador { get; set; }
        [JsonPropertyName("confianca")]
        public double Confianca { get; set; }
    }
}
