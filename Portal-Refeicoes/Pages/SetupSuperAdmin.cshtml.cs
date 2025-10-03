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
//            [Required(ErrorMessage = "O nome de usu�rio � obrigat�rio.")]
//            [Display(Name = "Nome de Usu�rio")]
//            public string Username { get; set; }

//            [Required(ErrorMessage = "A senha � obrigat�ria.")]
//            [DataType(DataType.Password)]
//            [MinLength(6, ErrorMessage = "A senha deve ter no m�nimo 6 caracteres.")]
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
//                SuccessMessage = "Usu�rio SuperAdmin criado com sucesso! Agora voc� pode fazer o login.";
//                ModelState.Clear(); // Limpa o formul�rio
//                return Page();
//            }
//            else
//            {
//                ErrorMessage = "N�o foi poss�vel criar o usu�rio. Um SuperAdmin j� pode existir ou a API est� indispon�vel.";
//                return Page();
//            }
//        }
//    }
//}