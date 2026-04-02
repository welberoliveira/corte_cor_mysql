using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class SalaoConfigFiscalModel : PageModel
    {
        private readonly SalaoConfigFiscalHandler _configHandler;
        private readonly ICriptografiaService _criptoService;

        public SalaoConfigFiscalModel(SalaoConfigFiscalHandler configHandler, ICriptografiaService criptoService)
        {
            _configHandler = configHandler;
            _criptoService = criptoService;
        }

        [BindProperty]
        public SalaoConfigFiscal Configuracao { get; set; } = new();

        public bool PossuiCertificadoSalvo { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string Erro { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var salaoIdStr = User.FindFirst("IdSalao")?.Value;
            if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

            var idSalao = int.Parse(salaoIdStr);
            Configuracao = await _configHandler.ObterPorSalaoAsync(idSalao) ?? new SalaoConfigFiscal { IdSalao = idSalao, Ambiente = 2 };
            PossuiCertificadoSalvo = Configuracao.CertificadoPfx != null && Configuracao.CertificadoPfx.Length > 0;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(IFormFile certificadoFile, string senhaCertificado)
        {
            var salaoIdStr = User.FindFirst("IdSalao")?.Value;
            if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

            try
            {
                Configuracao.IdSalao = int.Parse(salaoIdStr);
                Configuracao.Cnpj = Configuracao.Cnpj?.Replace(".", "", StringComparison.Ordinal).Replace("/", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
                Configuracao.EnderecoCep = Configuracao.EnderecoCep?.Replace("-", "", StringComparison.Ordinal);
                Configuracao.Telefone = Configuracao.Telefone?.Replace("(", "", StringComparison.Ordinal).Replace(")", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);

                var configAtual = await _configHandler.ObterPorSalaoAsync(Configuracao.IdSalao);
                if (certificadoFile != null && certificadoFile.Length > 0)
                {
                    if (string.IsNullOrEmpty(senhaCertificado))
                    {
                        Erro = "Senha do certificado é obrigatória quando um novo arquivo PFX é enviado.";
                        return Page();
                    }

                    using var ms = new MemoryStream();
                    await certificadoFile.CopyToAsync(ms);
                    Configuracao.CertificadoPfx = ms.ToArray();
                    Configuracao.CertificadoSenha = _criptoService.Criptografar(senhaCertificado);
                }
                else if (configAtual != null)
                {
                    Configuracao.CertificadoPfx = configAtual.CertificadoPfx;
                    Configuracao.CertificadoSenha = configAtual.CertificadoSenha;
                }

                Configuracao.DataAtualizacao = DateTime.Now;
                if (configAtual == null)
                {
                    await _configHandler.AddAsync(Configuracao);
                    Mensagem = "Configurações fiscais criadas com sucesso.";
                }
                else
                {
                    Configuracao.IdConfigFiscal = configAtual.IdConfigFiscal;
                    await _configHandler.UpdateAsync(Configuracao);
                    Mensagem = "Configurações fiscais atualizadas com sucesso.";
                }

                PossuiCertificadoSalvo = Configuracao.CertificadoPfx != null && Configuracao.CertificadoPfx.Length > 0;
            }
            catch (Exception ex)
            {
                Erro = "Ocorreu um erro ao salvar as configurações: " + ex.Message;
            }

            return Page();
        }
    }
}
