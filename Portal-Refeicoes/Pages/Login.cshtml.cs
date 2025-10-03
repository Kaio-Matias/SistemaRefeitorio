using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken] // <-- CORREÇÃO ADICIONADA AQUI
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public LoginModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        private class TokenResponse
        {
            public string Token { get; set; }
        }

        public class InputModel
        {
            [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
            [Display(Name = "Usuário")]
            public string Username { get; set; }

            [Required(ErrorMessage = "A senha é obrigatória.")]
            [DataType(DataType.Password)]
            [Display(Name = "Senha")]
            public string Password { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            ReturnUrl = returnUrl;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _clientFactory.CreateClient("ApiClient");
            var loginData = new { username = Input.Username, password = Input.Password };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Login ou senha inválidos.");
                return Page();
            }

            var responseStream = await response.Content.ReadAsStreamAsync();
            var tokenObject = await JsonSerializer.DeserializeAsync<TokenResponse>(responseStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var tokenString = tokenObject?.Token;

            if (string.IsNullOrEmpty(tokenString))
            {
                ModelState.AddModelError(string.Empty, "Token não recebido da API.");
                return Page();
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            var claimsFromToken = token.Claims;

            var userName = claimsFromToken.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ??
                           claimsFromToken.FirstOrDefault(c => c.Type == "unique_name")?.Value;
            var userRole = claimsFromToken.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim("access_token", tokenString) // Armazena o token para uso futuro no ApiClient
            };

            if (!string.IsNullOrEmpty(userRole))
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return LocalRedirect(ReturnUrl);
        }
    }
}