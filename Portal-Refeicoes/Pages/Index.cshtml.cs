using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Portal_Refeicoes.Pages
{
    [Authorize] // Garante que apenas usu�rios logados podem acessar esta p�gina
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // No futuro, voc� pode carregar dados para o dashboard aqui
        }
    }
}