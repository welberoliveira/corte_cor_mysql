using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    public class EsqueciSenhaModel : PageModel
    {
        private readonly IDatabaseHandler _dbHandler;

        public EsqueciSenhaModel(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string TipoUsuario { get; set; }

        public string Mensagem { get; set; }
        public string NomeCurto { get; set; }
        public string SalaoLink { get; set; }
        public string SalaoLinkID { get; set; }
        public string NomeSalao { get; set; }

        public void OnGet(string t, string? nomeSalaoLink)
        {
            ViewData["HideMenu"] = "true";
            TipoUsuario = t;

            if (nomeSalaoLink == null)
            {
                return;
            }

            SalaoLink = nomeSalaoLink;

            using var connection = _dbHandler.GetConnection();
            const string query = @"
                SELECT IdSalao
                FROM CorteCor_Salao
                WHERE NomeCurto = @SalaoLink AND Status = 'Ativo';";

            using var command = connection.CreateCommand();
            command.CommandText = query;
            command.AddWithValue("@SalaoLink", SalaoLink);
            _ = command.ExecuteScalar();
        }

        public void OnPost(string tipoCadastro, int idSalao)
        {
            ViewData["HideMenu"] = "true";
            TipoUsuario = tipoCadastro;
            if (string.IsNullOrWhiteSpace(TipoUsuario))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                Mensagem = "Por favor, selecione um tipo de usuario e informe um email valido.";
                return;
            }

            const string tabela = "CorteCor_Usuario";
            const string query = "SELECT Email FROM CorteCor_Usuario WHERE Email = @Email AND IdSalao = @IdSalao";

            string? usuarioEmail = null;
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@Email", Email);
                command.AddWithValue("@IdSalao", idSalao);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    usuarioEmail = reader["Email"]?.ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(usuarioEmail))
            {
                Mensagem = "E-mail nao encontrado.";
                return;
            }

            var novaSenha = GerarSenhaAleatoria();
            var senhaCriptografada = PasswordSecurity.HashPassword(novaSenha);

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"UPDATE {tabela} SET Senha = @Senha WHERE Email = @Email AND IdSalao = @IdSalao";
                command.AddWithValue("@Senha", senhaCriptografada);
                command.AddWithValue("@Email", Email);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }

            string from = "CorteCor@tonni.com.br";
            string subject = "Recuperacao de Senha - Corte & Cor";
            string body = $"<p>Ola,</p>" +
                          $"<p>Voce solicitou a recuperacao da sua senha no sistema <b>Corte & Cor</b>.</p>" +
                          $"<p>Sua nova senha temporaria e: <b>{novaSenha}</b></p>" +
                          $"<p>Recomendamos alterar a senha apos realizar o acesso.</p>" +
                          $"<p>Para acessar a pagina de autenticacao <a href='tonni.com.br/CorteCor/login/{NomeCurto}'>clique aqui</a>, ou acesse: <a href='tonni.com.br/CorteCor/login/{NomeCurto}'>tonni.com.br/CorteCor/login/{NomeCurto}</a></p>" +
                          $"<br><br><p>Atenciosamente, <br>Equipe Tonni Tecnologia <br> <a href='tonni.com.br'>tonni.com.br</a></p>";

            var loginManager = new LoginManager(_dbHandler);
            loginManager.EnviarEmail(Email, from, subject, body);
            Mensagem = "Uma nova senha foi enviada para o seu e-mail.";
        }

        private static string GerarSenhaAleatoria()
        {
            int senha;
            do
            {
                senha = Random.Shared.Next(123456, 999999);
            } while (senha < 123456);

            return senha.ToString();
        }
    }
}
