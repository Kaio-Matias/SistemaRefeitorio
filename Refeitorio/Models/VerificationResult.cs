using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refeitorio.Models
{
    public class VerificationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string ColaboradorNome { get; set; }
    }
}
