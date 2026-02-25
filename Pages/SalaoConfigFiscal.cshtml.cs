using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Services;

namespace CorteCor.Pages
{
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
        public SalaoConfigFiscal Configuracao { get; set; }

        public bool PossuiCertificadoSalvo { get; set; }
        public string Mensagem { get; set; }
        public string Erro { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var salaoIdStr = User.FindFirst("IdSalao")?.Value;
            if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

            var idSalao = int.Parse(salaoIdStr);

            // Fetch from database
            var configData = await _configHandler.ObterPorSalaoAsync(idSalao);
            Configuracao = configData ?? new SalaoConfigFiscal { IdSalao = idSalao, Ambiente = 2 };

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

                // Limpezas de input
                if (!string.IsNullOrEmpty(Configuracao.Cnpj)) Configuracao.Cnpj = Configuracao.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "");

                // Obter configuração existente
                var configAtual = await _configHandler.ObterPorSalaoAsync(Configuracao.IdSalao);

                // Processar upload de Certificado Novo
                if (certificadoFile != null && certificadoFile.Length > 0)
                {
                    if (string.IsNullOrEmpty(senhaCertificado))
                    {
                        Erro = "Senha do certificado Ã© obrigatÃ³ria quando um novo arquivo PFX Ã© enviado.";
                        return Page();
                    }

                    using (var ms = new MemoryStream())
                    {
                        await certificadoFile.CopyToAsync(ms);
                        Configuracao.CertificadoPfx = ms.ToArray();
                    }

                    // Encriptar a senha a ser salva com AES-256
                    Configuracao.CertificadoSenha = _criptoService.Criptografar(senhaCertificado);
                }
                else if (configAtual != null)
                {
                    // Manter o certificado antigo preservado caso a config jÃ¡ exista
                    Configuracao.CertificadoPfx = configAtual.CertificadoPfx;
                    Configuracao.CertificadoSenha = configAtual.CertificadoSenha;
                }

                Configuracao.DataAtualizacao = DateTime.Now;

                if (configAtual == null)
                {
                    await _configHandler.AddAsync(Configuracao);
                    Mensagem = "ConfiguraÃ§Ãµes Fiscais criadas com sucesso!";
                }
                else
                {
                    Configuracao.IdConfigFiscal = configAtual.IdConfigFiscal;
                    await _configHandler.UpdateAsync(Configuracao);
                    Mensagem = "ConfiguraÃ§Ãµes Fiscais atualizadas com sucesso!";
                }

                PossuiCertificadoSalvo = Configuracao.CertificadoPfx != null && Configuracao.CertificadoPfx.Length > 0;
            }
            catch (Exception ex)
            {
                Erro = "Ocorreu um erro ao salvar as configuraÃ§Ãµes: " + ex.Message;
            }

            return Page();
        }
    }
}
