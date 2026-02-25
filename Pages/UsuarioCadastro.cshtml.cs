using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Text;


namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class UsuarioCadastroModel : PageModel
    {
        public Usuario Usuario { get; set; }

        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }
        public string NomeClientes { get; set; }
        public string NomeCliente { get; set; }
        public string NomeCurto { get; set; }
        public List<Salao> Saloes { get; set; }


        public void OnGet(int? id)
        {
            var SalaoHandler = new SalaoHandler();
            Saloes = SalaoHandler.Listar();

            if (id.HasValue)
            {
                var handler = new UsuarioHandler();
                Usuario = handler.Listar().FirstOrDefault(m => m.IdUsuario == id.Value);
                ButtonText = "Atualizar";
            }
        }

        public void OnPost()
        {
            Random random = new Random();
            string senha = random.Next(123456, 987654).ToString();
            string senhaConvertida = Convert.ToBase64String(Encoding.UTF8.GetBytes(senha));

            int.TryParse(Request.Form["id"], out var id);
            int IdSaloeselecionada = int.Parse(Request.Form["IdSalao"]);

            var handler = new UsuarioHandler();
            var Usuario = new Usuario
            {
                IdUsuario = id,
                Nome = Request.Form["nome"],
                Sobrenome= Request.Form["sobrenome"],
                CPF= Request.Form["cpf"],
                Email = Request.Form["email"],
                Telefone = Request.Form["telefone"],
                DataEntrada = DateTime.Parse(Request.Form["dataEntrada"]),
                Status = "Ativo",
                Senha = senhaConvertida.ToString(),
                IdSalao = IdSaloeselecionada
            };

            if (id > 0)
            {
                handler.Atualizar(Usuario);
                Mensagem = $"Usuário atualizado com sucesso!";
            }
            else
            {
                if (handler.Listar().Where(m => m.Email == Usuario.Email).Any())
                {
                    Mensagem = $"Não foi possível finalizar o cadastro! Este email já está cadastrado: {Usuario.Email}";
                    OnGet(id > 0 ? id : null);
                    return;
                }

                id = handler.CadastrarUsuario(Usuario);
                Mensagem = $"Usuário cadastrado com sucesso!";
            }

            OnGet(id > 0 ? id : null);
        }

        public void OnPostEnviarCredenciais()
        {
            string email = Request.Form["email"];
            int.TryParse(Request.Form["id"], out var id);

            var handler = new UsuarioHandler();
            var Usuario = handler.Listar().Where(m => m.IdUsuario == id).ToList();


            string senhaCriptografada = Usuario.FirstOrDefault().Senha;
            byte[] senhaByte = Convert.FromBase64String(senhaCriptografada);
            var senhaRecuperada = Encoding.UTF8.GetString(senhaByte);

            // Enviar e-mail com a nova senha
            string from = "CorteCor@tonni.com.br";
            string subject = "Suas Credenciais - Tonni Corte & Cor";
            string body = $"<p>Olá, seja bem-vindo</p>" +
                          $"<p>O administrador do <b>Tonni Corte & Cor</b> criou um usuário para você nesse sistema.</p>" +
                          $"<p>O login é seu email: <b>{Usuario.FirstOrDefault().Email}</b></p>" +
                          $"<p>Sua senha é: <b>{senhaRecuperada}</b></p>" +
                          $"<p>Para acessar o sistema <a href='https://tonni.com.br/CorteCor'>clique aqui</a>, ou acesse: <a href='https://tonni.com.br/CorteCor'>tonni.com.br/CorteCor/adm</a></p>" +
                          $"<br><br><p>Atenciosamente, <br>Equipe Tonni Tecnologia <br> <a href='https://tonni.com.br'>tonni.com.br</a></p>";

            var loginManager = new LoginManager();
            loginManager.EnviarEmail(email, from, subject, body);

            Mensagem = $"A senha e instruções para acesso foram enviadas por email para: {email}. Lembre que o email pode estar no SPAM ou no Lixo Eletrônico.";
            OnGet(id > 0 ? id : null);
        }
    }
}


