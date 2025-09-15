using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace Portal_Refeicoes.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public LoginModel(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Se o usuário já está logado, redireciona para a home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _clientFactory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync("/api/auth/login", Input);

            if (response.IsSuccessStatusCode)
            {
                var responseStream = await response.Content.ReadAsStreamAsync();
                var authResult = await JsonSerializer.DeserializeAsync<AuthResult>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, authResult.User.Email),
                    new Claim(ClaimTypes.Name, authResult.User.Nome),
                    new Claim(ClaimTypes.Role, authResult.User.Role),
                    new Claim("access_token", authResult.Token) // Armazenamos o token aqui
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToPage("/Index");
            }
            else
            {
                ErrorMessage = "Falha no login. Verifique suas credenciais.";
                return Page();
            }
        }
    }

    // Classes auxiliares para deserializar a resposta do login
    public class AuthResult
    {
        public UserInfo User { get; set; }
        public string Token { get; set; }
    }

    public class UserInfo
    {
        public string Email { get; set; }
        public string Nome { get; set; }
        public string Role { get; set; }
    }
}