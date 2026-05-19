using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Logs;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PessoaCadastroModel : PageModel
    {
        private readonly ConsultaDocumentoService _consultaService;
        private readonly PessoaHandler _pessoaHandler;
        private readonly Log _logger = new Log();

        public Pessoa Pessoa { get; set; } = new Pessoa { IsCliente = true };
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "success";
        public List<string> TelefonesCadastro => SepararContatos(Pessoa?.Telefone);
        public List<string> EmailsCadastro => SepararContatos(Pessoa?.Email);

        public PessoaCadastroModel(ConsultaDocumentoService consultaService, PessoaHandler pessoaHandler)
        {
            _consultaService = consultaService;
            _pessoaHandler = pessoaHandler;
        }

        public void OnGet(int? id)
        {
            try
            {
                if (!id.HasValue)
                {
                    Pessoa = new Pessoa { IsCliente = true };
                    return;
                }

                int idSalao = 0;
                int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

                Pessoa = _pessoaHandler.ObterPorIdESalao(id.Value, idSalao) ?? new Pessoa { IsCliente = true };

                if (Pessoa.IdPessoa > 0)
                {
                    ButtonText = "Atualizar";
                    return;
                }

                Response.Redirect(HttpContext.Request.PathBase + "/PessoaLista");
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                Response.Redirect(HttpContext.Request.PathBase + "/Error");
            }
        }

        public IActionResult OnPost()
        {
            try
            {
                int.TryParse(Request.Form["id"], out var id);
                int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao);

                Pessoa = MapPessoaFromForm(id, idSalao);
                ButtonText = Pessoa.IdPessoa > 0 ? "Atualizar" : "Cadastrar";

                if (!ValidarPessoa(Pessoa))
                {
                    return Page();
                }

                if (id > 0)
                {
                    _pessoaHandler.Atualizar(Pessoa);
                    Mensagem = "Pessoa atualizada com sucesso!";
                }
                else
                {
                    Pessoa.IdPessoa = _pessoaHandler.CadastrarPessoa(Pessoa);
                    Mensagem = "Pessoa cadastrada com sucesso!";
                    ButtonText = "Atualizar";
                }

                MensagemTipo = "success";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                Mensagem = "Ocorreu um erro ao salvar os dados. O suporte tecnico foi notificado.";
                MensagemTipo = "danger";
                return Page();
            }
        }

        public async Task<IActionResult> OnGetConsultaCnpjAsync(string cnpj)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cnpj))
            return new JsonResult(new { sucesso = false, mensagem = "CNPJ não informado." });

                var resultado = await _consultaService.ConsultarCnpjAsync(cnpj);

                if (resultado == null)
            return new JsonResult(new { sucesso = false, mensagem = "CNPJ não encontrado ou inválido." });

                return new JsonResult(new
                {
                    sucesso = true,
                    razaoSocial = resultado.RazaoSocial ?? "",
                    nomeFantasia = resultado.NomeFantasia ?? "",
                    cnae = resultado.Cnae ?? "",
                    situacao = resultado.Situacao ?? "",
                    telefone = resultado.Telefone ?? "",
                    email = resultado.Email ?? "",
                    dataFundacao = resultado.Abertura ?? "",
                    logradouro = resultado.Logradouro ?? "",
                    numero = resultado.Numero ?? "",
                    complemento = resultado.Complemento ?? "",
                    bairro = resultado.Bairro ?? "",
                    cep = resultado.Cep ?? "",
                    cidade = resultado.Municipio ?? "",
                    uf = resultado.UF ?? ""
                });
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                return new JsonResult(new { sucesso = false, mensagem = "Erro ao consultar CNPJ." });
            }
        }

        public async Task<IActionResult> OnGetConsultaCepAsync(string cep)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cep))
            return new JsonResult(new { sucesso = false, mensagem = "CEP não informado." });

                var resultado = await _consultaService.ConsultarCepAsync(cep);

                if (resultado == null)
            return new JsonResult(new { sucesso = false, mensagem = "CEP não encontrado." });

                return new JsonResult(new
                {
                    sucesso = true,
                    logradouro = resultado.Logradouro ?? "",
                    bairro = resultado.Bairro ?? "",
                    cidade = resultado.Cidade ?? "",
                    uf = resultado.UF ?? ""
                });
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                return new JsonResult(new { sucesso = false, mensagem = "Erro ao consultar CEP." });
            }
        }

        public void OnPostEnviarCredenciais()
        {
            try
            {
                int.TryParse(Request.Form["id"], out var id);

                if (id <= 0)
                {
                    Mensagem = "Conclua e salve o cadastro antes de gerar os dados de acesso.";
                    MensagemTipo = "warning";
                    return;
                }

                string email = ObterPrimeiroContato(Request.Form["email"]);
                if (string.IsNullOrWhiteSpace(email))
                {
                    Mensagem = "O campo e-mail e obrigatorio para enviar credenciais.";
                    MensagemTipo = "warning";
                    OnGet(id);
                    return;
                }

                var pessoa = _pessoaHandler.ObterPorId(id);

                if (pessoa == null)
                {
                Mensagem = "Pessoa não encontrada.";
                    MensagemTipo = "danger";
                    return;
                }

                Random random = new Random();
                string senha = random.Next(123456, 987654).ToString();

                string from = "CorteCor@tonni.com.br";
                string subject = "Suas Credenciais - Tonni Corte & Cor";
                string body = $"<p>Ola {pessoa.Nome},</p>" +
                              $"<p>Seguem suas credenciais de acesso ao sistema <b>Tonni Corte & Cor</b>.</p>" +
                              $"<p>Login: <b>{email}</b></p>" +
                              $"<p>Senha: <b>{senha}</b></p>" +
                              $"<p>Para acessar o sistema <a href='https://tonni.com.br/CorteCor'>clique aqui</a></p>" +
                              $"<br><br><p>Atenciosamente, <br>Equipe Tonni Tecnologia <br> <a href='https://tonni.com.br'>tonni.com.br</a></p>";

                var loginManager = new LoginManager();
                loginManager.EnviarEmail(email, from, subject, body);

                Mensagem = $"As credenciais foram enviadas por email para: {email}. Verifique a caixa de SPAM.";
                MensagemTipo = "success";
                OnGet(id);
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                Mensagem = "Ocorreu um erro ao enviar as credenciais. O suporte tecnico foi notificado.";
                MensagemTipo = "danger";
            }
        }

        private Pessoa MapPessoaFromForm(int id, int idSalao)
        {
            DateTime? dataNascimento = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["dataNascimento"]))
                dataNascimento = DateTime.Parse(Request.Form["dataNascimento"]);

            DateTime? dataComemorativa = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["dataComemorativa"]))
                dataComemorativa = DateTime.Parse(Request.Form["dataComemorativa"]);

            int? indicadorIE = null;
            if (int.TryParse(Request.Form["indicadorIE"], out var ieParsed))
                indicadorIE = ieParsed;

            bool? consumidorFinal = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["consumidorFinal"]))
                consumidorFinal = Request.Form["consumidorFinal"] == "on" || Request.Form["consumidorFinal"] == "true" || Request.Form["consumidorFinal"] == "1";

            return new Pessoa
            {
                IdPessoa = id,
                IdSalao = idSalao,
                Nome = NormalizarTexto(Request.Form["nome"]) ?? string.Empty,
                Telefone = NormalizarTexto(Request.Form["telefone"]) ?? string.Empty,
                Email = NormalizarTexto(Request.Form["email"]) ?? string.Empty,
                DataNascimento = dataNascimento,
                CpfCnpj = LimparDocumento(Request.Form["cpfCnpj"]),
                InscricaoEstadual = NormalizarTexto(Request.Form["inscricaoEstadual"]),
                InscricaoMunicipal = NormalizarTexto(Request.Form["inscricaoMunicipal"]),
                Cep = LimparCep(Request.Form["cep"]),
                Logradouro = NormalizarTexto(Request.Form["logradouro"]),
                Numero = NormalizarTexto(Request.Form["numero"]),
                Complemento = NormalizarTexto(Request.Form["complemento"]),
                Bairro = NormalizarTexto(Request.Form["bairro"]),
                Cidade = NormalizarTexto(Request.Form["cidade"]),
                UF = NormalizarTexto(Request.Form["uf"]),
                RazaoSocial = NormalizarTexto(Request.Form["razaoSocial"]),
                NomeFantasia = NormalizarTexto(Request.Form["nomeFantasia"]),
                Cnae = NormalizarTexto(Request.Form["cnae"]),
                IsCliente = Request.Form["isCliente"] == "on" || Request.Form["isCliente"] == "true",
                IsFornecedor = Request.Form["isFornecedor"] == "on" || Request.Form["isFornecedor"] == "true",
                IsLead = Request.Form["isLead"] == "on" || Request.Form["isLead"] == "true",
                IsTransportador = false,
                NomeContato = NormalizarTexto(Request.Form["nomeContato"]),
                Pais = NormalizarTexto(Request.Form["pais"]),
                IdEstrangeiro = NormalizarTexto(Request.Form["idEstrangeiro"]),
                EntCep = LimparCep(Request.Form["entCep"]),
                EntUf = NormalizarTexto(Request.Form["entUf"]),
                EntCidade = NormalizarTexto(Request.Form["entCidade"]),
                EntNome = NormalizarTexto(Request.Form["entNome"]),
                EntCpfCnpj = LimparDocumento(Request.Form["entCpfCnpj"]),
                EntInscricaoEstadual = NormalizarTexto(Request.Form["entInscricaoEstadual"]),
                EntLogradouro = NormalizarTexto(Request.Form["entLogradouro"]),
                EntNumero = NormalizarTexto(Request.Form["entNumero"]),
                EntComplemento = NormalizarTexto(Request.Form["entComplemento"]),
                EntBairro = NormalizarTexto(Request.Form["entBairro"]),
                EntEmail = NormalizarTexto(Request.Form["entEmail"]),
                EntTelefone = NormalizarTexto(Request.Form["entTelefone"]),
                ConsumidorFinal = consumidorFinal,
                IndicadorIE = indicadorIE,
                IESubstTrib = NormalizarTexto(Request.Form["ieSubstTrib"]),
                Suframa = NormalizarTexto(Request.Form["suframa"]),
                Tags = NormalizarTexto(Request.Form["tags"]),
                DataComemorativa = dataComemorativa,
                DescricaoComemoracao = NormalizarTexto(Request.Form["descricaoComemoracao"]),
                BasesLegais = NormalizarTexto(Request.Form["basesLegais"]),
                Observacoes = NormalizarTexto(Request.Form["observacoes"])
            };
        }

        private bool ValidarPessoa(Pessoa pessoa)
        {
            if (string.IsNullOrWhiteSpace(pessoa.Nome))
            {
                Mensagem = "Informe o nome da pessoa.";
                MensagemTipo = "warning";
                return false;
            }

            if (!pessoa.IsCliente && !pessoa.IsFornecedor && !pessoa.IsLead)
            {
                Mensagem = "Selecione pelo menos um tipo de contato.";
                MensagemTipo = "warning";
                return false;
            }

            var emails = SepararContatos(pessoa.Email);
            var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (emails.Any(email => !emailValidator.IsValid(email)))
            {
                Mensagem = "Informe somente e-mails validos.";
                MensagemTipo = "warning";
                return false;
            }

            foreach (var telefone in SepararContatos(pessoa.Telefone))
            {
                var telefoneItemNumerico = new string(telefone.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrWhiteSpace(telefoneItemNumerico) && telefoneItemNumerico.Length is < 10 or > 11)
                {
                    Mensagem = "Informe telefones com DDD valido.";
                    MensagemTipo = "warning";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(pessoa.CpfCnpj) && pessoa.CpfCnpj.Length != 11 && pessoa.CpfCnpj.Length != 14)
            {
                Mensagem = "CPF/CNPJ deve conter 11 ou 14 digitos.";
                MensagemTipo = "warning";
                return false;
            }

            if (_pessoaHandler.ExisteCpfCnpjPorSalao(pessoa.CpfCnpj ?? string.Empty, pessoa.IdSalao, pessoa.IdPessoa > 0 ? pessoa.IdPessoa : null))
            {
                Mensagem = "Ja existe uma pessoa com este CPF/CNPJ.";
                MensagemTipo = "warning";
                return false;
            }

            foreach (var email in emails)
            {
                if (_pessoaHandler.ExisteEmailPorSalao(email, pessoa.IdSalao, pessoa.IdPessoa > 0 ? pessoa.IdPessoa : null))
                {
                    Mensagem = $"Ja existe uma pessoa com o e-mail {email}.";
                    MensagemTipo = "warning";
                    return false;
                }
            }

            return true;
        }

        private static string? LimparDocumento(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Replace(".", "").Replace("/", "").Replace("-", "").Trim();
        }

        private static string? LimparCep(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Replace("-", "").Trim();
        }

        private static string? NormalizarTexto(string? value)
        {
            var texto = value?.Trim();
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Normalize(NormalizationForm.FormC);
        }

        private static List<string> SepararContatos(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return new List<string>();
            }

            return valor
                .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ObterPrimeiroContato(string? valor) =>
            SepararContatos(valor).FirstOrDefault() ?? string.Empty;
    }
}
