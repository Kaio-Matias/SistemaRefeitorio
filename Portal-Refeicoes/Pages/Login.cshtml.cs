using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal_Refeicoes.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Portal_Refeicoes.Pages
{
    [AllowAnonymous] // Permite acesso a esta página sem estar logado
    public class LoginModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public LoginModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
            [Display(Name = "Usuário")]
            public string Username { get; set; }

            [Required(ErrorMessage = "A senha é obrigatória.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("/");
            }
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                var token = await _apiClient.LoginAsync(Input.Username, Input.Password);

                if (token != null)
                {
                    HttpContext.Session.SetString("JWToken", token);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, Input.Username),
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return LocalRedirect(ReturnUrl);
                }
                else
                {
                    ErrorMessage = "Usuário ou senha inválidos.";
                    return Page();
                }
            }

            return Page();
        }
    }
}