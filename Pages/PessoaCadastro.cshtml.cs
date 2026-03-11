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
        private readonly Log _logger = new Log();

        public Pessoa Pessoa { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }
        public string MensagemTipo { get; set; } = "success"; // success, danger, warning

        public PessoaCadastroModel(ConsultaDocumentoService consultaService)
        {
            _consultaService = consultaService;
        }

        public void OnGet(int? id)
        {
            try
            {
                if (id.HasValue)
                {
                    int idSalao = 0;
                    int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

                    var handler = new PessoaHandler();
                    Pessoa = handler.ObterPorId(id.Value);

                    if (Pessoa != null && Pessoa.IdSalao != idSalao)
                    {
                        Response.Redirect(HttpContext.Request.PathBase + $"/PessoaLista");
                        return;
                    }

                    ButtonText = "Atualizar";
                }
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                Response.Redirect(HttpContext.Request.PathBase + "/Error");
            }
        }

        public void OnPost()
        {
            try
            {
                int id = 0;
                int.TryParse(Request.Form["id"], out id);

                int idSalao = 0;
                int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

                DateTime? dataNascimento = null;
                if (!string.IsNullOrWhiteSpace(Request.Form["dataNascimento"]))
                    dataNascimento = DateTime.Parse(Request.Form["dataNascimento"]);

                bool isCliente = Request.Form["isCliente"] == "on" || Request.Form["isCliente"] == "true";
                bool isFornecedor = Request.Form["isFornecedor"] == "on" || Request.Form["isFornecedor"] == "true";
                bool isTransportador = Request.Form["isTransportador"] == "on" || Request.Form["isTransportador"] == "true";
                
                bool? consumidorFinal = null;
                if (!string.IsNullOrWhiteSpace(Request.Form["consumidorFinal"]))
                {
                    consumidorFinal = Request.Form["consumidorFinal"] == "on" || Request.Form["consumidorFinal"] == "true" || Request.Form["consumidorFinal"] == "1";
                }

                DateTime? dataComemorativa = null;
                if (!string.IsNullOrWhiteSpace(Request.Form["dataComemorativa"]))
                    dataComemorativa = DateTime.Parse(Request.Form["dataComemorativa"]);
                    
                int? indicadorIE = null;
                if (int.TryParse(Request.Form["indicadorIE"], out int ieParsed))
                    indicadorIE = ieParsed;

                var Pessoa = new Pessoa
                {
                    IdPessoa = id,
                    Nome = Request.Form["nome"],
                    Telefone = Request.Form["telefone"],
                    Email = Request.Form["email"],
                    DataNascimento = dataNascimento,
                    IdSalao = idSalao,
                    
                    // Campos Fiscais / Endereço
                    CpfCnpj = Request.Form["cpfCnpj"].ToString().Replace(".", "").Replace("/", "").Replace("-", ""),
                    InscricaoEstadual = Request.Form["inscricaoEstadual"],
                    InscricaoMunicipal = Request.Form["inscricaoMunicipal"],
                    Cep = Request.Form["cep"].ToString().Replace("-", ""),
                    Logradouro = Request.Form["logradouro"],
                    Numero = Request.Form["numero"],
                    Complemento = Request.Form["complemento"],
                    Bairro = Request.Form["bairro"],
                    Cidade = Request.Form["cidade"],
                    UF = Request.Form["uf"],

                    // Campos CNPJ (ReceitaWS)
                    RazaoSocial = Request.Form["razaoSocial"],
                    NomeFantasia = Request.Form["nomeFantasia"],
                    Cnae = Request.Form["cnae"],
                    
                    // Novos Campos
                    IsCliente = isCliente,
                    IsFornecedor = isFornecedor,
                    IsTransportador = isTransportador,
                    NomeContato = Request.Form["nomeContato"],
                    Pais = Request.Form["pais"],
                    IdEstrangeiro = Request.Form["idEstrangeiro"],
                    
                    EntCep = string.IsNullOrWhiteSpace(Request.Form["entCep"]) ? "" : Request.Form["entCep"].ToString().Replace("-", ""),
                    EntUf = Request.Form["entUf"],
                    EntCidade = Request.Form["entCidade"],
                    EntNome = Request.Form["entNome"],
                    EntCpfCnpj = string.IsNullOrWhiteSpace(Request.Form["entCpfCnpj"]) ? "" : Request.Form["entCpfCnpj"].ToString().Replace(".", "").Replace("/", "").Replace("-", ""),
                    EntInscricaoEstadual = Request.Form["entInscricaoEstadual"],
                    EntLogradouro = Request.Form["entLogradouro"],
                    EntNumero = Request.Form["entNumero"],
                    EntComplemento = Request.Form["entComplemento"],
                    EntBairro = Request.Form["entBairro"],
                    EntEmail = Request.Form["entEmail"],
                    EntTelefone = Request.Form["entTelefone"],
                    
                    ConsumidorFinal = consumidorFinal,
                    IndicadorIE = indicadorIE,
                    IESubstTrib = Request.Form["ieSubstTrib"],
                    Suframa = Request.Form["suframa"],
                    
                    Tags = Request.Form["tags"],
                    DataComemorativa = dataComemorativa,
                    DescricaoComemoracao = Request.Form["descricaoComemoracao"],
                    BasesLegais = Request.Form["basesLegais"],
                    Observacoes = Request.Form["observacoes"]
                };

                var handler = new PessoaHandler();

                if (id > 0)
                {
                    handler.Atualizar(Pessoa);
                    Mensagem = "Pessoa atualizada com sucesso!";
                    MensagemTipo = "success";
                }
                else
                {
                    id = handler.CadastrarPessoa(Pessoa);
                    Mensagem = "Pessoa cadastrada com sucesso!";
                    MensagemTipo = "success";
                }

                OnGet(id > 0 ? id : (int?)null);
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                Mensagem = "Ocorreu um erro ao salvar os dados. O suporte técnico foi notificado.";
                MensagemTipo = "danger";
            }
        }

        // ── Endpoint AJAX: Consultar CNPJ (ReceitaWS) ──
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

        // ── Endpoint AJAX: Consultar CEP (ViaCEP) ──
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

        // ── Botão "Enviar Credenciais" ──
        public void OnPostEnviarCredenciais()
        {
            try
            {
                int.TryParse(Request.Form["id"], out var id);

                // Regra de negócio: Impedir envio para cadastros novos (ID = 0)
                if (id <= 0)
                {
                    Mensagem = "Conclua e salve o cadastro antes de gerar os dados de acesso.";
                    MensagemTipo = "warning";
                    return;
                }

                string email = Request.Form["email"];
                if (string.IsNullOrWhiteSpace(email))
                {
                    Mensagem = "O campo e-mail é obrigatório para enviar credenciais.";
                    MensagemTipo = "warning";
                    OnGet(id);
                    return;
                }

                var handler = new PessoaHandler();
                var pessoa = handler.ObterPorId(id);

                if (pessoa == null)
                {
                    Mensagem = "Pessoa não encontrada.";
                    MensagemTipo = "danger";
                    return;
                }

                // Gerar senha aleatória
                Random random = new Random();
                string senha = random.Next(123456, 987654).ToString();

                // Enviar e-mail com as credenciais
                string from = "CorteCor@tonni.com.br";
                string subject = "Suas Credenciais - Tonni Corte & Cor";
                string body = $"<p>Olá {pessoa.Nome},</p>" +
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
                Mensagem = "Ocorreu um erro ao enviar as credenciais. O suporte técnico foi notificado.";
                MensagemTipo = "danger";
            }
        }
    }
}
