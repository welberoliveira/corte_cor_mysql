using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor.Services;
using CorteCor.Models;
using CorteCor.Handlers;

namespace CorteCor.Pages
{
    [Authorize]
    public class CertificadoDiagnosticoModel : PageModel
    {
        private readonly CertificadoFiscalFactory _certificadoFactory;
        private readonly SalaoConfigFiscalHandler _configHandler;

        public CertificadoDiagnosticoModel(
            CertificadoFiscalFactory certificadoFactory,
            SalaoConfigFiscalHandler configHandler)
        {
            _certificadoFactory = certificadoFactory;
            _configHandler = configHandler;
        }

        // Resultado do diagnÃ³stico
        public bool DiagnosticoRealizado { get; set; } = false;
        public bool CertificadoEncontrado { get; set; } = false;
        public bool CertificadoValido { get; set; } = false;
        public bool PossuiChavePrivada { get; set; } = false;
        public string Subject { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Thumbprint { get; set; } = "";
        public DateTime ValidoDe { get; set; }
        public DateTime ValidoAte { get; set; }
        public int DiasRestantes { get; set; } = 0;
        public string StatusCertificado { get; set; } = "";
        public string StatusCss { get; set; } = "";
        public string ErroMensagem { get; set; } = "";

        // Dados do Config Fiscal
        public string CnpjConfigurado { get; set; } = "";
        public string RazaoSocial { get; set; } = "";
        public int AmbienteConfigurado { get; set; } = 0;
        public DateTime? ValidadeArmazenada { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            DiagnosticoRealizado = true;

            try
            {
                var idSalaoStr = User.FindFirst("IdSalao")?.Value;
                if (string.IsNullOrEmpty(idSalaoStr))
                {
                    ErroMensagem = "Id da empresa não encontrado nos dados do usuário logado.";
                    return Page();
                }

                int idSalao = int.Parse(idSalaoStr);
                var config = await _configHandler.ObterPorSalaoAsync(idSalao);

                if (config == null)
                {
                    ErroMensagem = "Nenhuma configuração fiscal cadastrada para esta empresa. Acesse 'Configurações Fiscais' primeiro.";
                    return Page();
                }

                CnpjConfigurado = config.Cnpj ?? "(não informado)";
                RazaoSocial = config.RazaoSocial ?? "(não informado)";
                AmbienteConfigurado = config.Ambiente;
                ValidadeArmazenada = config.CertificadoValidade;

                if (config.CertificadoPfx == null || config.CertificadoPfx.Length == 0)
                {
                    ErroMensagem = "Nenhum arquivo de certificado digital (.pfx) foi enviado nas ConfiguraÃ§Ãµes Fiscais.";
                    return Page();
                }

                if (config.CertificadoSenha == null || config.CertificadoSenha.Length == 0)
                {
                    ErroMensagem = "A senha do certificado digital nÃ£o foi configurada.";
                    return Page();
                }

                // Tenta instanciar o certificado pelo mesmo pipeline da aplicaÃ§Ã£o
                X509Certificate2 cert;
                try
                {
                    cert = _certificadoFactory.InstanciarCertificado(config);
                }
                catch (Exception exCert)
                {
                    ErroMensagem = $"Falha ao carregar o certificado: {exCert.Message}. Verifique se a senha estÃ¡ correta e o arquivo .pfx nÃ£o estÃ¡ corrompido.";
                    return Page();
                }

                CertificadoEncontrado = true;
                Subject = cert.Subject;
                Issuer = cert.Issuer;
                SerialNumber = cert.SerialNumber;
                Thumbprint = cert.Thumbprint;
                ValidoDe = cert.NotBefore;
                ValidoAte = cert.NotAfter;
                PossuiChavePrivada = cert.HasPrivateKey;
                DiasRestantes = (int)(cert.NotAfter - DateTime.Now).TotalDays;

                if (DateTime.Now < cert.NotBefore)
                {
                    StatusCertificado = "â³ AINDA NÃƒO VÃLIDO (data de inÃ­cio no futuro)";
                    StatusCss = "warning";
                    CertificadoValido = false;
                }
                else if (DateTime.Now > cert.NotAfter)
                {
                    StatusCertificado = "âŒ VENCIDO";
                    StatusCss = "danger";
                    CertificadoValido = false;
                }
                else if (DiasRestantes <= 30)
                {
                    StatusCertificado = $"âš ï¸ VÃLIDO, mas VENCE EM {DiasRestantes} DIAS";
                    StatusCss = "warning";
                    CertificadoValido = true;
                }
                else
                {
                    StatusCertificado = $"âœ… VÃLIDO ({DiasRestantes} dias restantes)";
                    StatusCss = "success";
                    CertificadoValido = true;
                }

                if (!cert.HasPrivateKey)
                {
                    StatusCertificado += " | âŒ SEM CHAVE PRIVADA (assinatura XML falharÃ¡)";
                    StatusCss = "danger";
                    CertificadoValido = false;
                }

                cert.Dispose();
            }
            catch (Exception ex)
            {
                ErroMensagem = $"Erro inesperado durante o diagnÃ³stico: {ex.Message}";
            }

            return Page();
        }
    }
}


