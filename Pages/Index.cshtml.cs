using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Security.Claims;
using static CorteCor.Models;
using CorteCor;

namespace CorteCor.Pages
{
    public class LoginModel : PageModel
    {
        public string ErrorMessage { get; set; }
        public string NomeSalao { get; set; }

        public void OnGet()
        {
            ViewData["HideMenu"] = "true";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["HideMenu"] = "true";

            var dbHandler = new DatabaseHandler();

            var email = Request.Form["email"].ToString();
            var password = Request.Form["password"].ToString();

            var loginManager = new LoginManager();

            if (loginManager.AutenticarUsuario(email, password))
            {
                //buscar IdUsuario
                using var connection = dbHandler.GetConnection();
                string query = @"
                    SELECT IdUsuario
                    FROM CorteCor_Usuario 
                    WHERE Email = @Email;";

                using var command = connection.CreateCommand();
                command.CommandText = query;
                command.AddWithValue("@Email", email);
                var result = command.ExecuteScalar();

                string IdUsuario = "";
                if (result != null) IdUsuario = Convert.ToString(result);
                else
                {
                    ErrorMessage = "Ocorreu um erro inesperado";
                    return Page();
                }

                var handler = new UsuarioHandler();
                var Usuario = handler.Listar().FirstOrDefault(m => m.IdUsuario.ToString() == IdUsuario);


                // Criar os claims do usuário autenticado
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim("IdUsuario", IdUsuario),
                    new Claim("IdSalao", Usuario.IdSalao.ToString()),
                    new Claim("Role", "Usuario"),
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync("CookieAuth", claimsPrincipal);

                // ---- Verificação de Limite de Emails ----
                try {
                    var lembreteHandler = new LembreteHandler(dbHandler);
                    if (lembreteHandler.VerificarLimiteEmail(Usuario.IdSalao, out int enviados, out int limite))
                    {
                        TempData["AvisoLimite"] = $"Atenção: O limite de disparos de emails para seu salão foi alcançado ({enviados}/{limite}). Adquira mais créditos para continuar enviando lembretes.";
                    }
                }
                catch (Exception ex)
                {
                    // Não impedir login por erro aqui, apenas logar ou ignorar
                    Console.WriteLine("Erro ao verificar limite: " + ex.Message);
                }
                // -----------------------------------------

                // Redirecionar para a página inicial ou outra página protegida
                return Redirect(HttpContext.Request.PathBase + "/Agendamentos2");
            }
            else
            {
                ErrorMessage = "Email ou senha incorretos.";
                return Page();
            }
        }
    }
}
