using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class AdmAlterarSenhaModel : PageModel
    {
        private readonly IDatabaseHandler _dbHandler;

        public AdmAlterarSenhaModel(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public string Mensagem { get; set; } = "";

        [BindProperty]
        public string SenhaAtual { get; set; }

        [BindProperty]
        public string EmailUsuario { get; set; }

        [BindProperty]
        public string NovaSenha { get; set; }

        [BindProperty]
        public string ConfirmarSenha { get; set; }

        public void OnGet()
        {
            EmailUsuario = User.Identity?.Name;
        }

        public IActionResult OnPost()
        {
            var emailUsuario = User.Identity?.Name;
            EmailUsuario = emailUsuario;

            if (string.IsNullOrWhiteSpace(emailUsuario))
            {
                Mensagem = "Usuario nao identificado.";
                return Page();
            }

            if (NovaSenha != ConfirmarSenha)
            {
                Mensagem = "As senhas nao coincidem!";
                return Page();
            }

            using (var connection = _dbHandler.GetConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Senha FROM CorteCor_Usuario WHERE Email = @Email;";
                    command.AddWithValue("@Email", emailUsuario);
                    string senhaAtualDb = command.ExecuteScalar()?.ToString();

                    if (!PasswordSecurity.VerifyPassword(SenhaAtual, senhaAtualDb))
                    {
                        Mensagem = "Senha atual incorreta!";
                        return Page();
                    }
                }

                using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.CommandText = "UPDATE CorteCor_Usuario SET Senha = @NovaSenha WHERE Email = @Email";
                    updateCommand.AddWithValue("@NovaSenha", PasswordSecurity.HashPassword(NovaSenha));
                    updateCommand.AddWithValue("@Email", emailUsuario);
                    updateCommand.ExecuteNonQuery();
                }
            }

            string from = "CorteCor@tonni.com.br";
            string subject = "Alteracao de Senha - Corte & Cor";
            string body = $"<p>Ola,</p>" +
                          $"<p>Voce alterou sua senha no sistema <b>Corte & Cor</b>.</p>" +
                          $"<p>Se voce nao reconhece essa alteracao, altere a senha novamente e revise os acessos da conta.</p>" +
                          $"<p>Para acessar o sistema <a href='https://tonni.com.br/CorteCor'>clique aqui</a></p>" +
                          $"<br><br><p>Atenciosamente, <br>Equipe Tonni Tecnologia <br> <a href='https://tonni.com.br'>tonni.com.br</a></p>";

            var loginManager = new LoginManager(_dbHandler);
            loginManager.EnviarEmail(emailUsuario, from, subject, body);

            Mensagem = "Senha alterada com sucesso! Verifique seu e-mail.";
            return Page();
        }
    }
}
