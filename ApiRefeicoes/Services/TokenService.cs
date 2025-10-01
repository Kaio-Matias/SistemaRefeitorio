// Este ficheiro não precisa de alterações.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiRefeicoes.Models;
using Microsoft.IdentityModel.Tokens;

namespace ApiRefeicoes.Services
{
    public static class TokenService
    {
        public static string GenerateToken(Usuario user, IConfiguration configuration)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // A chave é lida aqui dentro.
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(8), // Token válido por 8 horas
                // Issuer e Audience também são lidos aqui.
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}