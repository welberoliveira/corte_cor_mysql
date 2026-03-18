using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor.Services;
using CorteCor.Models;
using CorteCor.Handlers;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class NotaFiscalEmitirModel : PageModel
    {
        private readonly NotaFiscalAvulsaService _notaFiscalAvulsaService;
        private readonly SalaoConfigFiscalHandler _configLogHandler;
        private readonly NotaFiscalLogHandler _sistemaLog;
        private readonly IValidaParametrosMunicipioService _validaParametrosMunicipioService;

        public NotaFiscalEmitirModel(
            NotaFiscalAvulsaService notaFiscalAvulsaService,
            SalaoConfigFiscalHandler configLogHandler,
            NotaFiscalLogHandler sistemaLog,
            IValidaParametrosMunicipioService validaParametrosMunicipioService)
        {
            _notaFiscalAvulsaService = notaFiscalAvulsaService;
            _configLogHandler = configLogHandler;
            _sistemaLog = sistemaLog;
            _validaParametrosMunicipioService = validaParametrosMunicipioService;
        }

        [BindProperty]
        public EmitirNfseViewModel Input { get; set; } = new EmitirNfseViewModel();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var idSalaoClaim = User.FindFirst("IdSalao")?.Value;
            var userName = User.Identity?.Name ?? "Sistema";

            if (string.IsNullOrEmpty(idSalaoClaim) || !int.TryParse(idSalaoClaim, out int idSalao))
            {
                return RedirectToPage("/Index");
            }

            try
            {
                var config = await _configLogHandler.ObterPorSalaoAsync(idSalao);
                if (config == null || string.IsNullOrEmpty(config.InscricaoMunicipal))
                {
                    TempData["ErrorMessage"] = "ConfiguraÃ§Ã£o Fiscal do SalÃ£o nÃ£o encontrada ou incompleta. Verifique a InscriÃ§Ã£o Municipal.";
                    return Page();
                }

                var cliente = new CorteCor.Models.Pessoa { Nome = Input.CpfCnpjDestinatario == null ? "Consumidor Final" : Input.NomeRazaoSocial, CpfCnpj = Input.CpfCnpjDestinatario };
                var servico = new CorteCor.Models.Servico { Nome = Input.DescricaoServico, Preco = Input.ValorServico, CodigoTributacaoMunicipio = Input.CodigoTributacaoNacional };
                var age = new CorteCor.Models.Agendamento { DataHora = DateTime.Now };

                await _validaParametrosMunicipioService.ValidateAsync(config, servico);
                var result = await _notaFiscalAvulsaService.EmitirAsync(idSalao, new NotaFiscalAvulsaRequest
                {
                    Modelo = "NFSE",
                    Ambiente = config.Ambiente,
                    Serie = config.SerieNFSe,
                    Numero = config.NumeroNFSe,
                    DataEmissao = age.DataHora,
                    EmitenteCnpj = config.Cnpj,
                    EmitenteNome = config.RazaoSocial,
                    EmitenteIM = config.InscricaoMunicipal,
                    EmitenteIE = config.InscricaoEstadual,
                    EmitenteCRT = config.RegimeTributario == 0 ? 1 : config.RegimeTributario,
                    EmitenteLogradouro = config.EnderecoLogradouro,
                    EmitenteNumero = config.EnderecoNumero,
                    EmitenteBairro = config.EnderecoBairro,
                    EmitenteCep = config.EnderecoCep,
                    EmitenteCidade = config.EnderecoCidade,
                    EmitenteUF = config.EnderecoUF,
                    EmitenteCodMun = config.CodigoMunicipioIBGE,
                    DestinatarioNome = cliente.Nome,
                    DestinatarioCpfCnpj = cliente.CpfCnpj,
                    Itens = new List<NotaFiscalAvulsaItemRequest>
                    {
                        new NotaFiscalAvulsaItemRequest
                        {
                            XProd = servico.Nome,
                            VUnCom = servico.Preco,
                            CodigoTributacao = servico.CodigoTributacaoMunicipio
                        }
                    }
                }, userName);

                // Loga Global
                await _sistemaLog.InserirAsync(new NotaFiscalLog
                {
                    IdSalao = idSalao,
                    IdNotaFiscal = result.IdNotaFiscal,
                    TipoEvento = result.MensagemTipo == "success" ? "EmissaoNFSeNacional_Sucesso" : "EmissaoNFSeNacional_Rejeicao",
                    RequestPayload = result.XmlEnvio,
                    ResponsePayload = result.XmlRetorno,
                    Mensagem = result.Mensagem,
                    CodigoErro = result.MensagemTipo == "success" ? null : "Erro Sefaz Nacional",
                    Usuario = userName
                });

                if (result.MensagemTipo == "success")
                {
                    TempData["SuccessMessage"] = "NFS-e emitida com sucesso! " + result.Mensagem;
                    return RedirectToPage("/NotaFiscalLogLista");
                }
                else
                {
                    TempData["ErrorMessage"] = "Falha ao emitir NFS-e Nacional: " + result.Mensagem;
                    return Page();
                }
            }
            catch (Exception ex)
            {
                // Unhandled logic exception
                await _sistemaLog.InserirAsync(new NotaFiscalLog
                {
                    IdSalao = idSalao,
                    TipoEvento = "EmissaoNFSeNacional_Exception",
                    Mensagem = ex.Message,
                    CodigoErro = "Exception Interna",
                    Usuario = userName
                });

                TempData["ErrorMessage"] = "Erro interno ao gerar NFS-e PadrÃ£o Nacional. Verifique a tela de Logs do Sistema.";
                return Page();
            }
        }
    }

    public class EmitirNfseViewModel
    {
        [Display(Name = "CPF/CNPJ Tomador (Opcional)")]
        public string CpfCnpjDestinatario { get; set; }

        [Display(Name = "Nome/RazÃ£o Social Tomador")]
        public string NomeRazaoSocial { get; set; }

        [Required(ErrorMessage = "O Valor do ServiÃ§o Ã© obrigatÃ³rio")]
        [Display(Name = "Valor do ServiÃ§o (R$)")]
        public decimal ValorServico { get; set; }

        [Required(ErrorMessage = "A DescriÃ§Ã£o do ServiÃ§o Ã© obrigatÃ³ria")]
        [Display(Name = "DescriÃ§Ã£o do ServiÃ§o")]
        public string DescricaoServico { get; set; }

        [Required(ErrorMessage = "O CÃ³digo de TributaÃ§Ã£o Nacional Ã© obrigatÃ³rio (ex: 06.01)")]
        [Display(Name = "CÃ³d. TributaÃ§Ã£o Nacional")]
        public string CodigoTributacaoNacional { get; set; }
    }
}

