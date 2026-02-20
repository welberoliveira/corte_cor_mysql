using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using CorteCor;


namespace CorteCor.Pages
{
    public class EsqueciSenhaModel : PageModel
    {
        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string TipoUsuario { get; set; }

        public string Mensagem { get; set; }
        
        public string NomeCurto { get; set; }
        public string SalaoLink { get; set; }
        public string SalaoLinkID { get; set; }
        public string NomeSalao { get; set; }
        public IDatabaseHandler dbHandler = new DatabaseHandler();

        public void OnGet(string t, string? nomeSalaoLink)
        {
            ViewData["HideMenu"] = "true";

            if (string.IsNullOrEmpty(nomeSalaoLink)) 
            {
                 // Logic to handle missing link if needed
            }

            TipoUsuario = t;

            int IdSalao = 1;
            if (nomeSalaoLink != null)
            {
                SalaoLink = nomeSalaoLink;

                using var connection = dbHandler.GetConnection();
                string query = @"
                    SELECT IdSalao 
                    FROM CorteCor_Salao 
                    WHERE NomeCurto = @SalaoLink AND Status = 'Ativo';";

                using var command = connection.CreateCommand();
                command.CommandText = query;
                command.AddWithValue("@SalaoLink", SalaoLink);
                var result = command.ExecuteScalar();

                if (result != null) IdSalao = Convert.ToInt32(result);
            }
        }

        public void OnPost(string tipoCadastro, int IdSalao)
        {
            ViewData["HideMenu"] = "true";

            TipoUsuario = tipoCadastro;
            if (string.IsNullOrEmpty(TipoUsuario)) return;

            if (string.IsNullOrEmpty(Email))
            {
                Mensagem = "Por favor, selecione um tipo de usuário e insira um e-mail válido.";
                return;
            }

            object usuario = null;
            string tabela = "CorteCor_Usuario";

            // Buscar o usuário na tabela correspondente
            string query = $"SELECT Email FROM {tabela} WHERE Email = @Email and IdSalao = @IdSalao";
            using (var connection = dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@Email", Email);
                command.AddWithValue("@IdSalao", IdSalao);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        usuario = reader["Email"].ToString();
                    }
                }
            }

            if (usuario == null)
            {
                Mensagem = "E-mail não encontrado.";
                return;
            }

            string novaSenha = GerarSenhaAleatoria();
            string senhaCriptografada = Convert.ToBase64String(Encoding.UTF8.GetBytes(novaSenha));

            string updateQuery = $"UPDATE {tabela} SET Senha = @Senha WHERE Email = @Email and IdSalao = @IdSalao";
            using (var connection = dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = updateQuery;
                command.AddWithValue("@Senha", senhaCriptografada);
                command.AddWithValue("@Email", Email);
                command.AddWithValue("@IdSalao", IdSalao);
                command.ExecuteNonQuery();
            }

            string from = "CorteCor@tonni.com.br";
            string subject = "Recuperação de Senha - Corte & Cor";
            string body = $"<p>Olá,</p>" +
                          $"<p>Você solicitou a recuperação da sua senha no sistema <b>Corte & Cor</b>.</p>" +
                          $"<p>Sua nova senha é: <b>{novaSenha}</b></p>" +
                          $"<p>É recomendado alterar sua senha, clicando no botão \"Alterar Senha\", após realizar seu acesso.</p>" +
                          $"<p>Para acessar a página de autenticação <a href='tonni.com.br/CorteCor/login/{NomeCurto}'>clique aqui</a>, ou acesso: <a href='tonni.com.br/CorteCor/login/{NomeCurto}'>tonni.com.br/CorteCor/login/{NomeCurto}</a></p>" +
                          $"<br><br><p>Atenciosamente, <br>Equipe Tonni Tecnologia <br> <a href='tonni.com.br'>tonni.com.br</a></p>" +
                          $"<p></p>";

            var loginManager = new LoginManager();
            loginManager.EnviarEmail(Email, from, subject, body);
            Mensagem = "Uma nova senha foi enviada para o seu e-mail.";
        }

        private string GerarSenhaAleatoria()
        {
            Random random = new Random();
            int senha;
            do
            {
                senha = random.Next(123456, 999999);
            } while (senha < 123456);

            return senha.ToString();
        }
    }
}


