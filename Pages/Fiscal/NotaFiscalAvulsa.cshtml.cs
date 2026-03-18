using System.ComponentModel.DataAnnotations;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Fiscal
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class NotaFiscalAvulsaModel : PageModel
    {
        private readonly NotaFiscalAvulsaService _notaFiscalAvulsaService;

        public NotaFiscalAvulsaModel(NotaFiscalAvulsaService notaFiscalAvulsaService)
        {
            _notaFiscalAvulsaService = notaFiscalAvulsaService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel : IValidatableObject
        {
            [Display(Name = "Certificado Digital (.pfx)")]
            public IFormFile? CertificadoFile { get; set; }

            [Display(Name = "Senha do Certificado")]
            [DataType(DataType.Password)]
            public string? CertificadoSenha { get; set; }

            [Display(Name = "Ambiente")]
            [Range(1, 2, ErrorMessage = "Selecione um ambiente fiscal valido.")]
            public int Ambiente { get; set; } = 2;

            [Required]
            [Display(Name = "Modelo")]
            public string Modelo { get; set; } = "NFSE";

            [Required]
            [Display(Name = "Natureza da Operacao")]
            public string NaturezaOperacao { get; set; } = "Prestacao de servico";

            [Range(1, int.MaxValue, ErrorMessage = "A serie deve ser maior que zero.")]
            public int Serie { get; set; } = 1;

            [Range(1, int.MaxValue, ErrorMessage = "O numero da nota deve ser maior que zero.")]
            public int Numero { get; set; } = 1;

            public DateTime DataEmissao { get; set; } = DateTime.Now;

            [Required]
            [Display(Name = "CNPJ do Emitente")]
            public string? EmitenteCnpj { get; set; }

            [Required]
            [Display(Name = "Razao Social")]
            public string? EmitenteNome { get; set; }

            [Display(Name = "Inscricao Estadual")]
            public string? EmitenteIE { get; set; }

            [Display(Name = "Inscricao Municipal")]
            public string? EmitenteIM { get; set; }

            public int EmitenteCRT { get; set; } = 1;
            public string? EmitenteLogradouro { get; set; }
            public string? EmitenteNumero { get; set; }
            public string? EmitenteBairro { get; set; }
            public string? EmitenteCep { get; set; }
            public string? EmitenteCidade { get; set; }
            public string? EmitenteUF { get; set; }
            public int EmitenteCodMun { get; set; }

            [Required]
            [Display(Name = "CPF/CNPJ do Destinatario")]
            public string? DestinatarioCpfCnpj { get; set; }

            [Required]
            [Display(Name = "Nome / Razao Social")]
            public string? DestinatarioNome { get; set; }

            [Display(Name = "Inscricao Estadual")]
            public string? DestinatarioIE { get; set; }

            public string? DestinatarioLogradouro { get; set; }
            public string? DestinatarioNumero { get; set; }
            public string? DestinatarioBairro { get; set; }
            public string? DestinatarioCep { get; set; }
            public string? DestinatarioCidade { get; set; }
            public string? DestinatarioUF { get; set; }
            public int DestinatarioCodMun { get; set; }

            [EmailAddress(ErrorMessage = "Informe um e-mail valido para o destinatario.")]
            public string? DestinatarioEmail { get; set; }

            public List<NotaFiscalAvulsaItem> Itens { get; set; } = new();
            public Guid? IdNotaFiscal { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (DataEmissao > DateTime.Now.AddMinutes(5))
                {
                    yield return new ValidationResult(
                        "A data de emissao nao pode estar muito a frente do horario atual.",
                        new[] { nameof(DataEmissao) });
                }

                if (EmitenteCodMun <= 0)
                {
                    yield return new ValidationResult(
                        "Informe o codigo do municipio do emitente.",
                        new[] { nameof(EmitenteCodMun) });
                }

                if (DestinatarioCodMun <= 0)
                {
                    yield return new ValidationResult(
                        "Informe o codigo do municipio do destinatario.",
                        new[] { nameof(DestinatarioCodMun) });
                }

                if (Itens.Count == 0)
                {
                    yield return new ValidationResult(
                        "Informe ao menos um item para emitir a nota.",
                        new[] { nameof(Itens) });
                }

                for (var index = 0; index < Itens.Count; index++)
                {
                    foreach (var erro in Itens[index].Validar(index))
                    {
                        yield return erro;
                    }
                }

                var tipoNota = NotaFiscalAvulsaService.InferirTipoNota(Modelo);
                if (string.Equals(tipoNota, "NFS-e", StringComparison.OrdinalIgnoreCase))
                {
                    if (Itens.Count > 1)
                    {
                        yield return new ValidationResult(
                            "A tela avulsa de NFS-e trabalha com um item por emissao nesta fase do projeto.",
                            new[] { nameof(Itens) });
                    }

                    if (Itens.Count > 0 && string.IsNullOrWhiteSpace(Itens[0].CodigoTributacao))
                    {
                        yield return new ValidationResult(
                            "Informe o codigo de tributacao do servico para NFS-e.",
                            new[] { $"{nameof(Itens)}[0].{nameof(NotaFiscalAvulsaItem.CodigoTributacao)}" });
                    }
                }
            }
        }

        public class NotaFiscalAvulsaItem
        {
            public string CProd { get; set; } = "1";
            public string XProd { get; set; } = string.Empty;
            public string NCM { get; set; } = "00";
            public string CFOP { get; set; } = "5102";
            public string uCom { get; set; } = "UN";
            public decimal qCom { get; set; } = 1;
            public decimal vUnCom { get; set; }
            public decimal vProd { get; set; }
            public string CSOSN { get; set; } = "102";
            public decimal vTotTrib { get; set; }
            public string? CodigoTributacao { get; set; }
            public decimal AliquotaISS { get; set; } = 5;

            public IEnumerable<ValidationResult> Validar(int index)
            {
                if (string.IsNullOrWhiteSpace(XProd))
                {
                    yield return new ValidationResult(
                        "Informe a descricao do item fiscal.",
                        new[] { $"{nameof(NotaFiscalAvulsaModel.InputModel.Itens)}[{index}].{nameof(XProd)}" });
                }

                if (qCom <= 0)
                {
                    yield return new ValidationResult(
                        "A quantidade do item deve ser maior que zero.",
                        new[] { $"{nameof(NotaFiscalAvulsaModel.InputModel.Itens)}[{index}].{nameof(qCom)}" });
                }

                if (vUnCom < 0)
                {
                    yield return new ValidationResult(
                        "O valor unitario do item nao pode ser negativo.",
                        new[] { $"{nameof(NotaFiscalAvulsaModel.InputModel.Itens)}[{index}].{nameof(vUnCom)}" });
                }

                if (AliquotaISS < 0)
                {
                    yield return new ValidationResult(
                        "A aliquota de ISS nao pode ser negativa.",
                        new[] { $"{nameof(NotaFiscalAvulsaModel.InputModel.Itens)}[{index}].{nameof(AliquotaISS)}" });
                }
            }
        }

        public class SelectModel
        {
            public string Value { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "info";
        public string ProtocoloAutorizacao { get; set; } = string.Empty;
        public string XmlRetorno { get; set; } = string.Empty;
        public string XmlEnvio { get; set; } = string.Empty;
        public string ChaveAcesso { get; set; } = string.Empty;
        public Guid? IdNotaFiscalEmitida { get; set; }
        public bool HasSavedCertificate { get; set; }
        public List<string> Logs { get; set; } = new();
        public List<NotaFiscal> NotasEmitidas { get; set; } = new();
        public List<NotaFiscalInutilizacao> Inutilizacoes { get; set; } = new();
        public NotaFiscalRetornoResumo? RetornoResumoAtual { get; set; }
        public NotaFiscal? NotaHistoricoSelecionada { get; set; }
        public NotaFiscalRetornoResumo? RetornoResumoHistorico { get; set; }
        public List<NotaFiscalEvento> EventosHistorico { get; set; } = new();
        public List<NotaFiscalLog> LogsHistorico { get; set; } = new();

        public List<SelectModel> ModelosList { get; set; } = new()
        {
            new SelectModel { Value = "55", Text = "55 - NF-e (Mercadorias)" },
            new SelectModel { Value = "65", Text = "65 - NFC-e (Cupom Fiscal)" },
            new SelectModel { Value = "NFSE", Text = "NFS-e (Servicos)" }
        };

        public List<string> NaturezasList { get; set; } = new()
        {
            "Venda de mercadoria",
            "Prestacao de servico",
            "Devolucao",
            "Remessa",
            "Outras"
        };

        public List<SelectModel> MunicipiosList { get; set; } = new()
        {
            new SelectModel { Value = "3143302", Text = "Montes Claros - MG" },
            new SelectModel { Value = "3550308", Text = "Sao Paulo - SP" },
            new SelectModel { Value = "3304557", Text = "Rio de Janeiro - RJ" },
            new SelectModel { Value = "5300108", Text = "Brasilia - DF" }
        };

        public async Task OnGetAsync()
        {
            await CarregarContextoAsync();
        }

        public async Task<IActionResult> OnPostConsultarAsync(string chaveAcesso)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                var identificadorFiscal = string.IsNullOrWhiteSpace(chaveAcesso) ? ChaveAcesso : chaveAcesso;
                var resultado = await _notaFiscalAvulsaService.ConsultarAsync(idSalao, Input.Modelo, Input.Ambiente, identificadorFiscal);
                AplicarResultado(resultado);
                await CarregarContextoAsync();
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("consultar nota", ex);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSugerirNumeroAsync()
        {
            await CarregarContextoAsync();
            var idSalao = ObterIdSalaoAtual();
            var contexto = await _notaFiscalAvulsaService.ObterContextoTelaAsync(idSalao, Input.Modelo, Input.Ambiente, Input.Serie, 0);
            var tipoNota = NotaFiscalAvulsaService.InferirTipoNota(Input.Modelo);
            Input.Numero = contexto.NumeroSugerido;
            Logs.Add($"Numero sugerido para {tipoNota}: {Input.Numero}");
            return Page();
        }

        public async Task<IActionResult> OnPostCancelarAsync(string chaveAcesso, string justificativa)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                var resultado = await _notaFiscalAvulsaService.CancelarAsync(idSalao, chaveAcesso, justificativa);
                AplicarResultado(resultado);
                await CarregarContextoAsync();
                await RecarregarHistoricoAsync(idSalao, chaveAcesso);
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("cancelar nota", ex);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDownloadXmlDbAsync(string chaveAcesso, string tipo)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                var xml = await _notaFiscalAvulsaService.ObterXmlAsync(idSalao, chaveAcesso, tipo);
                return File(xml.Bytes, "text/xml", xml.FileName);
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("baixar XML da nota", ex);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostInutilizarAsync(int ano, int serie, int numInicial, int numFinal, string justificativa, string tipoNota)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                var resultado = await _notaFiscalAvulsaService.InutilizarAsync(idSalao, ano, serie, numInicial, numFinal, justificativa, tipoNota);
                AplicarResultado(resultado);
                await CarregarContextoAsync();
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("inutilizar faixa", ex);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGerarPdfAsync(string chaveAcesso)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                var pdf = await _notaFiscalAvulsaService.GerarPdfAsync(idSalao, chaveAcesso);
                return File(pdf.Bytes, "application/pdf", pdf.FileName);
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("gerar PDF da nota", ex);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEnviarEmailAsync(string chaveAcesso, string emailDestino, string? nomeDestino)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                var resultado = await _notaFiscalAvulsaService.EnviarEmailAsync(idSalao, chaveAcesso, emailDestino, nomeDestino);
                AplicarResultado(resultado);
                await CarregarContextoAsync();
                await RecarregarHistoricoAsync(idSalao, chaveAcesso);
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("enviar e-mail da nota", ex);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVerHistoricoAsync(string chaveAcesso)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                await RecarregarHistoricoAsync(idSalao, chaveAcesso);
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("carregar historico da nota", ex);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCartaCorrecaoAsync(string chaveAcesso, string textoCorrecao)
        {
            await CarregarContextoAsync();
            try
            {
                var idSalao = ObterIdSalaoAtual();
                var resultado = await _notaFiscalAvulsaService.CartaCorrecaoAsync(idSalao, chaveAcesso, textoCorrecao);
                AplicarResultado(resultado);
                await CarregarContextoAsync();
                await RecarregarHistoricoAsync(idSalao, chaveAcesso);
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("enviar carta de correcao", ex);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await CarregarContextoAsync();
            var isAutoSubmit = Request.Form.ContainsKey("__auto") || !Request.Form.ContainsKey("btnEmitir");
            var idSalao = ObterIdSalaoAtual();

            if (isAutoSubmit)
            {
                var tipoNota = NotaFiscalAvulsaService.InferirTipoNota(Input.Modelo);
                var contexto = await _notaFiscalAvulsaService.ObterContextoTelaAsync(idSalao, Input.Modelo, Input.Ambiente, Input.Serie, 0);
                Input.Numero = contexto.NumeroSugerido;
                Logs.Add($"Auto-sugestao para {tipoNota}: {Input.Numero}");
                ModelState.Clear();
                return Page();
            }

            if (!ModelState.IsValid)
            {
                Mensagem = "Revise os dados obrigatorios antes de emitir.";
                MensagemTipo = "warning";
                return Page();
            }

            try
            {
                var request = await MapearRequestAsync();
                var resultado = await _notaFiscalAvulsaService.EmitirAsync(idSalao, request, User.Identity?.Name);
                AplicarResultado(resultado);
                await CarregarContextoAsync();
                TempData["SuccessMessage"] = resultado.Mensagem;
            }
            catch (Exception ex)
            {
                RegistrarErroOperacao("transmitir nota", ex);
            }

            return Page();
        }

        private async Task CarregarContextoAsync()
        {
            var idSalao = ObterIdSalaoAtual();
            var contexto = await _notaFiscalAvulsaService.ObterContextoTelaAsync(idSalao, Input.Modelo, Input.Ambiente, Input.Serie, Input.Numero);

            Input.Ambiente = contexto.Ambiente;
            Input.Serie = contexto.Serie;
            Input.Numero = contexto.NumeroSugerido;
            Input.EmitenteCnpj ??= contexto.EmitenteCnpj;
            Input.EmitenteNome ??= contexto.EmitenteNome;
            Input.EmitenteIE ??= contexto.EmitenteIE;
            Input.EmitenteIM ??= contexto.EmitenteIM;
            Input.EmitenteCRT = Input.EmitenteCRT == 1 && contexto.EmitenteCRT > 1 ? contexto.EmitenteCRT : Input.EmitenteCRT;
            Input.EmitenteLogradouro ??= contexto.EmitenteLogradouro;
            Input.EmitenteNumero ??= contexto.EmitenteNumero;
            Input.EmitenteBairro ??= contexto.EmitenteBairro;
            Input.EmitenteCep ??= contexto.EmitenteCep;
            Input.EmitenteCidade ??= contexto.EmitenteCidade;
            Input.EmitenteUF ??= contexto.EmitenteUF;
            Input.EmitenteCodMun = Input.EmitenteCodMun == 0 ? contexto.EmitenteCodMun : Input.EmitenteCodMun;
            HasSavedCertificate = contexto.HasSavedCertificate;

            if (Input.Itens.Count == 0)
            {
                Input.Itens.Add(new NotaFiscalAvulsaItem
                {
                    XProd = contexto.ItemPadrao.XProd,
                    qCom = contexto.ItemPadrao.QCom,
                    vUnCom = contexto.ItemPadrao.VUnCom,
                    CodigoTributacao = contexto.ItemPadrao.CodigoTributacao,
                    AliquotaISS = contexto.ItemPadrao.AliquotaISS
                });
            }

            NotasEmitidas = contexto.NotasEmitidas;
            Inutilizacoes = contexto.Inutilizacoes;
            if (TempData["SuccessMessage"] is string success && string.IsNullOrWhiteSpace(Mensagem))
            {
                Mensagem = success;
                MensagemTipo = "success";
            }

            if (TempData["ErrorMessage"] is string error && string.IsNullOrWhiteSpace(Mensagem))
            {
                Mensagem = error;
                MensagemTipo = "danger";
            }
        }

        private async Task<NotaFiscalAvulsaRequest> MapearRequestAsync()
        {
            byte[]? pfxBytes = null;
            if (Input.CertificadoFile != null)
            {
                using var ms = new MemoryStream();
                await Input.CertificadoFile.CopyToAsync(ms);
                pfxBytes = ms.ToArray();
            }

            return new NotaFiscalAvulsaRequest
            {
                CertificadoPfxBytes = pfxBytes,
                CertificadoSenha = Input.CertificadoSenha,
                Ambiente = Input.Ambiente,
                Modelo = Input.Modelo,
                NaturezaOperacao = Input.NaturezaOperacao,
                Serie = Input.Serie,
                Numero = Input.Numero,
                DataEmissao = Input.DataEmissao,
                EmitenteCnpj = Input.EmitenteCnpj,
                EmitenteNome = Input.EmitenteNome,
                EmitenteIE = Input.EmitenteIE,
                EmitenteIM = Input.EmitenteIM,
                EmitenteCRT = Input.EmitenteCRT,
                EmitenteLogradouro = Input.EmitenteLogradouro,
                EmitenteNumero = Input.EmitenteNumero,
                EmitenteBairro = Input.EmitenteBairro,
                EmitenteCep = Input.EmitenteCep,
                EmitenteCidade = Input.EmitenteCidade,
                EmitenteUF = Input.EmitenteUF,
                EmitenteCodMun = Input.EmitenteCodMun,
                DestinatarioCpfCnpj = Input.DestinatarioCpfCnpj,
                DestinatarioNome = Input.DestinatarioNome,
                DestinatarioIE = Input.DestinatarioIE,
                DestinatarioLogradouro = Input.DestinatarioLogradouro,
                DestinatarioNumero = Input.DestinatarioNumero,
                DestinatarioBairro = Input.DestinatarioBairro,
                DestinatarioCep = Input.DestinatarioCep,
                DestinatarioCidade = Input.DestinatarioCidade,
                DestinatarioUF = Input.DestinatarioUF,
                DestinatarioCodMun = Input.DestinatarioCodMun,
                DestinatarioEmail = Input.DestinatarioEmail,
                Itens = Input.Itens.Select(item => new NotaFiscalAvulsaItemRequest
                {
                    CProd = item.CProd,
                    XProd = item.XProd,
                    NCM = item.NCM,
                    CFOP = item.CFOP,
                    UCom = item.uCom,
                    QCom = item.qCom,
                    VUnCom = item.vUnCom,
                    VProd = item.vProd,
                    CSOSN = item.CSOSN,
                    VTotTrib = item.vTotTrib,
                    CodigoTributacao = item.CodigoTributacao,
                    AliquotaISS = item.AliquotaISS
                }).ToList()
            };
        }

        private void AplicarResultado(NotaFiscalOperacaoResult resultado)
        {
            Mensagem = resultado.Mensagem;
            MensagemTipo = resultado.MensagemTipo;
            ProtocoloAutorizacao = NotaFiscalAvulsaService.SanitizarProtocoloFiscal(resultado.ProtocoloAutorizacao) ?? string.Empty;
            XmlRetorno = resultado.XmlRetorno ?? string.Empty;
            XmlEnvio = resultado.XmlEnvio ?? string.Empty;
            ChaveAcesso = PrimeiroValorPreenchido(
                resultado.ChaveAcesso,
                resultado.NotaFiscal?.ChaveAcessoNacional,
                resultado.NotaFiscal?.ChaveAcesso,
                resultado.RetornoResumo?.ChaveAcesso);
            IdNotaFiscalEmitida = resultado.IdNotaFiscal;
            RetornoResumoAtual = resultado.RetornoResumo
                ?? NotaFiscalAvulsaService.CriarResumoRetorno(
                    resultado.XmlRetorno,
                    resultado.ProtocoloAutorizacao,
                    resultado.ChaveAcesso,
                    resultado.NotaFiscal?.Status);
            Logs = resultado.Logs.ToList();
        }

        private void AplicarHistorico(NotaFiscalHistoricoResult historico)
        {
            NotaHistoricoSelecionada = historico.NotaFiscal;
            RetornoResumoHistorico = historico.RetornoResumo;
            EventosHistorico = historico.Eventos;
            LogsHistorico = historico.Logs;
        }

        private static string PrimeiroValorPreenchido(params string?[] valores)
        {
            foreach (var valor in valores)
            {
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    return valor;
                }
            }

            return string.Empty;
        }

        private int ObterIdSalaoAtual()
        {
            if (int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) && idSalao > 0)
            {
                return idSalao;
            }

            throw new InvalidOperationException("Nao foi possivel identificar o salao atual para operar o modulo fiscal.");
        }

        private async Task RecarregarHistoricoAsync(int idSalao, string? chaveAcesso)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso))
            {
                return;
            }

            var historico = await _notaFiscalAvulsaService.ObterHistoricoAsync(idSalao, chaveAcesso);
            AplicarHistorico(historico);
        }

        private void RegistrarErroOperacao(string operacao, Exception ex)
        {
            Mensagem = $"Erro ao {operacao}: {ex.Message}";
            MensagemTipo = "danger";
            Logs.Add(Mensagem);
        }
    }
}
