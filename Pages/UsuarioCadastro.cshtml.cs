using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class UsuarioCadastroModel : PageModel
    {
        private readonly IDatabaseHandler _dbHandler;

        public UsuarioCadastroModel(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public Usuario Usuario { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }
        public string NomeClientes { get; set; }
        public string NomeCliente { get; set; }
        public string NomeCurto { get; set; }
        public List<Salao> Saloes { get; set; } = new();

        public void OnGet(int? id)
        {
            var salaoHandler = new SalaoHandler(_dbHandler);
            Saloes = salaoHandler.Listar();

            if (id.HasValue)
            {
                var handler = new UsuarioHandler(_dbHandler);
                Usuario = handler.Listar().FirstOrDefault(m => m.IdUsuario == id.Value);
                ButtonText = "Atualizar";
            }
        }

        public void OnPost()
        {
            var senhaTemporaria = Random.Shared.Next(123456, 987654).ToString();

            int.TryParse(Request.Form["id"], out var id);
            int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalaoSelecionada);

            var handler = new UsuarioHandler(_dbHandler);
            var usuario = new Usuario
            {
                IdUsuario = id,
                Nome = Request.Form["nome"],
                Sobrenome = Request.Form["sobrenome"],
                CPF = Request.Form["cpf"],
                Email = Request.Form["email"],
                Telefone = Request.Form["telefone"],
                DataEntrada = DateTime.Parse(Request.Form["dataEntrada"]),
                Status = "Ativo",
                Senha = PasswordSecurity.HashPassword(senhaTemporaria),
                IdSalao = idSalaoSelecionada
            };

            if (id > 0)
            {
                handler.Atualizar(usuario);
                Mensagem = "Usuario atualizado com sucesso!";
            }
            else
            {
                var queryCheckEmail = "SELECT COUNT(1) FROM CorteCor_Usuario WHERE Email = @Email";
                using var connection = _dbHandler.GetConnection();
                using var command = connection.CreateCommand();
                command.CommandText = queryCheckEmail;
                command.AddWithValue("@Email", usuario.Email);
                var emailExiste = Convert.ToInt32(command.ExecuteScalar()) > 0;

                if (emailExiste)
                {
                    Mensagem = $"Nao foi possivel finalizar o cadastro! Este email ja esta cadastrado: {usuario.Email}";
                    OnGet(id > 0 ? id : null);
                    return;
                }

                id = handler.CadastrarUsuario(usuario);
                Mensagem = "Usuario cadastrado com sucesso!";
            }

            OnGet(id > 0 ? id : null);
        }

        public void OnPostEnviarCredenciais()
        {
            string email = Request.Form["email"];
            int.TryParse(Request.Form["id"], out var id);

            var handler = new UsuarioHandler(_dbHandler);
            var usuario = handler.ObterPorId(id);
            if (usuario == null)
            {
                Mensagem = "Usuario nao encontrado para envio das credenciais.";
                OnGet(id > 0 ? id : null);
                return;
            }

            var senhaTemporaria = Random.Shared.Next(123456, 987654).ToString();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE CorteCor_Usuario SET Senha = @Senha WHERE IdUsuario = @IdUsuario";
                command.AddWithValue("@Senha", PasswordSecurity.HashPassword(senhaTemporaria));
                command.AddWithValue("@IdUsuario", id);
                command.ExecuteNonQuery();
            }

            string from = "CorteCor@tonni.com.br";
            string subject = "Suas Credenciais - Tonni Corte & Cor";
            string body = $"<p>Ola, seja bem-vindo</p>" +
                          $"<p>O administrador do <b>Tonni Corte & Cor</b> criou um usuario para voce nesse sistema.</p>" +
                          $"<p>O login e seu email: <b>{usuario.Email}</b></p>" +
                          $"<p>Sua senha temporaria e: <b>{senhaTemporaria}</b></p>" +
                          $"<p>Altere a senha no primeiro acesso para manter a conta protegida.</p>" +
                          $"<p>Para acessar o sistema <a href='https://tonni.com.br/CorteCor'>clique aqui</a>, ou acesse: <a href='https://tonni.com.br/CorteCor'>tonni.com.br/CorteCor/adm</a></p>" +
                          $"<br><br><p>Atenciosamente, <br>Equipe Tonni Tecnologia <br> <a href='https://tonni.com.br'>tonni.com.br</a></p>";

            var loginManager = new LoginManager(_dbHandler);
            loginManager.EnviarEmail(email, from, subject, body);

            Mensagem = $"As credenciais foram enviadas por email para: {email}. Lembre que o email pode estar no SPAM ou no lixo eletronico.";
            OnGet(id > 0 ? id : null);
        }
    }
}
