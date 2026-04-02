using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using CorteCor.Logs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Security.Claims;

using CorteCor;

namespace CorteCor.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IDatabaseHandler _dbHandler;

        public LoginModel(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public string ErrorMessage { get; set; }
        public string NomeSalao { get; set; }

        public void OnGet()
        {
            ViewData["HideMenu"] = "true";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ViewData["HideMenu"] = "true";

            var email = Request.Form["email"].ToString();
            var password = Request.Form["password"].ToString();

            var loginManager = new LoginManager();

            if (loginManager.AutenticarUsuario(email, password))
            {
                // Registrar acesso bem-sucedido
                try
                {
                    var ipOrigem = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
                    new LogAcessoHandler().Registrar(email, ipOrigem, "Usuario", true);
                }
                catch { /* NÃ£o impedir login */ }
                //buscar IdUsuario
                using var connection = _dbHandler.GetConnection();
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
                var Usuario = handler.ObterPorId(int.Parse(IdUsuario));


                // Criar os claims do usuÃ¡rio autenticado
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

                // ---- VerificaÃ§Ã£o de Limites (Email e SMS) ----
                try {
                    var lembreteHandler = new LembreteHandler(_dbHandler);
                    
                    // Email
                    if (lembreteHandler.VerificarLimiteEmail(Usuario.IdSalao, out int envEmail, out int limEmail))
                    {
                        TempData["AvisoLimite"] = $"Atenção: O limite de disparos de e-mails para sua empresa foi alcançado ({envEmail}/{limEmail}). Adquira mais crÃ©ditos para continuar enviando lembretes por email.";
                    }

                    // SMS
                    if (lembreteHandler.VerificarLimiteSMS(Usuario.IdSalao, out int envSms, out int limSms))
                    {
                        TempData["AvisoLimiteSMS"] = $"Atenção: O limite de disparos de SMS para sua empresa foi alcançado ({envSms}/{limSms}). Adquira mais crÃ©ditos para continuar enviando lembretes por SMS.";
                    }
                }
                catch (Exception ex)
                {
                    // NÃ£o impedir login por erro aqui, apenas logar ou ignorar
                    Console.WriteLine("Erro ao verificar limites: " + ex.Message);
                }
                // ----------------------------------------------

                // Redirecionar para o dashboard inicial
                return Redirect(HttpContext.Request.PathBase + "/Dashboard");
            }
            else
            {
                ErrorMessage = "Email ou senha incorretos.";

                // Registrar tentativa falha
                try
                {
                    var ipOrigem = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
                    new LogAcessoHandler().Registrar(email, ipOrigem, "Usuario", false);
                }
                catch { /* NÃ£o impedir fluxo */ }

                return Page();
            }
        }
    }
}




