using CorteCor.Handlers;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using CorteCor;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class UsuarioAlterarSenhaModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public string Mensagem { get; set; } = "";

        public UsuarioAlterarSenhaModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string SenhaAtual { get; set; }
        [BindProperty]
        public string EmailUsuario { get; set; }

        [BindProperty]
        public string NovaSenha { get; set; }

        [BindProperty]
        public string ConfirmarSenha { get; set; }
        public string NomeCurto { get; set; }

        public void OnGet()
        {
            ViewData["HideMenu"] = "true";
            EmailUsuario = User.Identity.Name;
        }

        public IActionResult OnPost()
        {
            ViewData["HideMenu"] = "true";

            string emailUsuario = User.Identity.Name;
            EmailUsuario = User.Identity.Name;

            if (NovaSenha != ConfirmarSenha)
            {
                Mensagem = "As senhas não coincidem!";
                return Page();
            }

            DatabaseHandler dbHandler = new();
            using (var connection = dbHandler.GetConnection())
            {
                string query = "SELECT Senha FROM CorteCor_Usuario WHERE Email = @Email";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.AddWithValue("@Email", emailUsuario);
                    string senhaAtualDB = command.ExecuteScalar()?.ToString();

                    if (senhaAtualDB == null || senhaAtualDB != ConvertToBase64(SenhaAtual))
                    {
                        Mensagem = "Senha atual incorreta!";
                        return Page();
                    }
                }

                // Atualizar a senha no banco de dados
                string updateQuery = "UPDATE CorteCor_Usuario SET Senha = @NovaSenha WHERE Email = @Email";
                using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.CommandText = updateQuery;
                    updateCommand.AddWithValue("@NovaSenha", ConvertToBase64(NovaSenha));
                    updateCommand.AddWithValue("@Email", emailUsuario);
                    updateCommand.ExecuteNonQuery();
                }

                // Enviar e-mail com a nova senha
                string from = "CorteCor@tonni.com.br";
                string subject = "Alteração de Senha - Corte & Cor";
                string body = $"<p>Olá,</p>" +
                              $"<p>Você solicitou a alteração da sua senha no sistema <b>Corte & Cor</b>.</p>" +
                              $"<p>Sua senha foi alterada para: <b>{NovaSenha}</b></p>" +
                              $"<p>Para acessar a página de autenticação <a href='https://tonni.com.br/CorteCor/login/{NomeCurto}'>clique aqui</a></p>" +
                              $"<br><br><p>Atenciosamente, <br>Equipe Tonni Tecnologia <br> <a href='https://tonni.com.br'>tonni.com.br</a></p>";

                var loginManager = new LoginManager();
                loginManager.EnviarEmail(emailUsuario, from, subject, body);
            }

            Mensagem = "Senha alterada com sucesso! Verifique seu e-mail. Lembre que o email pode estar no SPAM ou no Lixo Eletrônico.";
            return Page();
        }

        private string ConvertToBase64(string input)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

    }
}


