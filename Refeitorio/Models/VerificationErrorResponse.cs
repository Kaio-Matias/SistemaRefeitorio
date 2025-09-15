using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Refeitorio.Models
{
    public class VerificationErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
