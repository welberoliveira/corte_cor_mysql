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
    [Authorize]
    public class NotaFiscalEmitirModel : PageModel
    {
        private readonly NFSeEmissorService _emissorService;
        private readonly FiscalBuilderService _builderService;
        private readonly SalaoConfigFiscalHandler _configLogHandler;
        private readonly NotaFiscalLogHandler _sistemaLog;
        private readonly IValidaParametrosMunicipioService _validaParametrosMunicipioService;

        public NotaFiscalEmitirModel(
            NFSeEmissorService emissorService, 
            FiscalBuilderService builderService,
            SalaoConfigFiscalHandler configLogHandler,
            NotaFiscalLogHandler sistemaLog,
            IValidaParametrosMunicipioService validaParametrosMunicipioService)
        {
            _emissorService = emissorService;
            _builderService = builderService;
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
                    TempData["ErrorMessage"] = "Configuração Fiscal do Salão não encontrada ou incompleta. Verifique a Inscrição Municipal.";
                    return Page();
                }

                // Mock objetos temporários apenas para usar o construtor existente
                var cliente = new CorteCor.Models.Pessoa { Nome = Input.CpfCnpjDestinatario == null ? "Consumidor Final" : Input.NomeRazaoSocial, CpfCnpj = Input.CpfCnpjDestinatario };
                var servico = new CorteCor.Models.Servico { Nome = Input.DescricaoServico, Preco = Input.ValorServico, CodigoTributacaoMunicipio = Input.CodigoTributacaoNacional };
                var age = new CorteCor.Models.Agendamento { DataHora = DateTime.Now };

                // Build DPS (Nacional)
                await _validaParametrosMunicipioService.ValidateAsync(config, servico);
                var dps = _builderService.MontarNFSe(config, cliente, servico, age);
                
                // Emite NFS-e (Nota: A assinatura espera int? idAgendamento como 3º argumento e o nome é EmitirNFSeAsync)
                var result = await _emissorService.EmitirNFSeAsync(config, dps, null);

                // Loga Global
                await _sistemaLog.InserirAsync(new NotaFiscalLog
                {
                    IdSalao = idSalao,
                    TipoEvento = result.Autorizada ? "EmissaoNFSeNacional_Sucesso" : "EmissaoNFSeNacional_Rejeicao",
                    RequestPayload = result.XmlEnvio,
                    ResponsePayload = result.XmlRetorno,
                    Mensagem = result.Motivo ?? "Processamento concluído",
                    CodigoErro = result.Autorizada ? null : "Erro Sefaz Nacional",
                    Usuario = userName
                });

                if (result.Autorizada)
                {
                    TempData["SuccessMessage"] = "NFS-e Nacional ou RPS Lote enviado com sucesso! " + result.Motivo;
                    return RedirectToPage("/NotaFiscalLogLista");
                }
                else
                {
                    TempData["ErrorMessage"] = "Falha ao emitir NFS-e Nacional: " + result.Motivo;
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

                TempData["ErrorMessage"] = "Erro interno ao gerar NFS-e Padrão Nacional. Verifique a tela de Logs do Sistema.";
                return Page();
            }
        }
    }

    public class EmitirNfseViewModel
    {
        [Display(Name = "CPF/CNPJ Tomador (Opcional)")]
        public string CpfCnpjDestinatario { get; set; }

        [Display(Name = "Nome/Razão Social Tomador")]
        public string NomeRazaoSocial { get; set; }

        [Required(ErrorMessage = "O Valor do Serviço é obrigatório")]
        [Display(Name = "Valor do Serviço (R$)")]
        public decimal ValorServico { get; set; }

        [Required(ErrorMessage = "A Descrição do Serviço é obrigatória")]
        [Display(Name = "Descrição do Serviço")]
        public string DescricaoServico { get; set; }

        [Required(ErrorMessage = "O Código de Tributação Nacional é obrigatório (ex: 06.01)")]
        [Display(Name = "Cód. Tributação Nacional")]
        public string CodigoTributacaoNacional { get; set; }
    }
}
