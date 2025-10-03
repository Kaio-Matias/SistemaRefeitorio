//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Portal_Refeicoes.Services;
//using System.ComponentModel.DataAnnotations;
//using System.Threading.Tasks;

//namespace Portal_Refeicoes.Pages
//{
//    [AllowAnonymous]
//    public class SetupSuperAdminModel : PageModel
//    {
//        private readonly ApiClient _apiClient;

//        public SetupSuperAdminModel(ApiClient apiClient)
//        {
//            _apiClient = apiClient;
//        }

//        [BindProperty]
//        public InputModel Input { get; set; }

//        public string SuccessMessage { get; set; }
//        public string ErrorMessage { get; set; }

//        public class InputModel
//        {
//            [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
//            [Display(Name = "Nome de Usuário")]
//            public string Username { get; set; }

//            [Required(ErrorMessage = "A senha é obrigatória.")]
//            [DataType(DataType.Password)]
//            [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
//            public string Senha { get; set; }
//        }

//        public void OnGet()
//        {
//        }

//        public async Task<IActionResult> OnPostAsync()
//        {
//            if (!ModelState.IsValid)
//            {
//                return Page();
//            }

//            var success = await _apiClient.CreateSuperAdminAsync(Input.Username, Input.Senha);

//            if (success)
//            {
//                SuccessMessage = "Usuário SuperAdmin criado com sucesso! Agora você pode fazer o login.";
//                ModelState.Clear(); // Limpa o formulário
//                return Page();
//            }
//            else
//            {
//                ErrorMessage = "Não foi possível criar o usuário. Um SuperAdmin já pode existir ou a API está indisponível.";
//                return Page();
//            }
//        }
//    }
//}