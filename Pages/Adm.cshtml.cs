using CorteCor.Models;
using CorteCor.Logs;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Security.Claims;


namespace CorteCor.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IDatabaseHandler _dbHandler;

        public IndexModel(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public string ErrorMessage { get; set; }

        [BindProperty]
        public string NomeSalao{ get; set; }
        public string NomeCliente{ get; set; }

        [BindProperty]
        public string SalaoLink { get; set; }
        public string SalaoLinkID { get; set; }

        public void OnGet(string nomeSalaoLink)
        {
            ViewData["HideMenu"] = "true";
        }

        public async Task<IActionResult> OnPostAsync(int IdSalao = 0)
        {
            ViewData["HideMenu"] = "true";

            var email = Request.Form["email"].ToString();
            var password = Request.Form["password"].ToString();
            var loginManager = new LoginManager(_dbHandler);

            // Tenta autenticar o usuário usando email e senha (a query interna do LoginManager também filtra por IdSalao)
                        if (loginManager.AutenticarAdministrador(email, password))
            {
                // Registrar acesso bem-sucedido
                try
                {
                    var ipOrigem = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
                    new LogAcessoHandler().Registrar(email, ipOrigem, "Admin", true);
                }
                catch { }

                // Cria os claims do usuário autenticado, incluindo o IdSalao
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim("Role", "Admin"),
                    new Claim("IdSalao", IdSalao.ToString())
                 };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Efetua o login via Cookie Authentication
                await HttpContext.SignInAsync("CookieAuth", claimsPrincipal);

                // Redireciona para a p?gina protegida (por exemplo, o Painel)
                return Redirect(HttpContext.Request.PathBase + "/Painel");
            }
            else
            {
                                ErrorMessage = "Email ou senha incorretos.";

                // Registrar tentativa falha
                try
                {
                    var ipOrigem = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
                    new LogAcessoHandler().Registrar(email, ipOrigem, "Admin", false);
                }
                catch { }

                return Page();
            }
        }
    }
}


