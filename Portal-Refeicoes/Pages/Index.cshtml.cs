using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Portal_Refeicoes.Pages
{
    [Authorize] // Garante que apenas usuários logados podem acessar esta página
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // No futuro, você pode carregar dados para o dashboard aqui
        }
    }
}