using ApiRefeicoes.Models;
using Microsoft.Extensions.Configuration; // Adicionar este using
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiRefeicoes.Services
{
    public static class TokenService
    {
        // Modificamos o método para receber IConfiguration
        public static string GenerateToken(Usuario user, IConfiguration configuration)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // Lendo a chave do appsettings.json para mais segurança
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    // Adiciona o nome de usuário (essencial para o 'Olá, @User.Identity.Name!')
                    new Claim(ClaimTypes.Name, user.Username),

                    // --- ESTA É A CORREÇÃO CRÍTICA E FINAL ---
                    // Adiciona o perfil (Role) do usuário ao token.
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}