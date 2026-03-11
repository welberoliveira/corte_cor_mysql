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

        // Resultado do diagnóstico
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
                    ErroMensagem = "IdSalao não encontrado nos Claims do usuário logado.";
                    return Page();
                }

                int idSalao = int.Parse(idSalaoStr);
                var config = await _configHandler.ObterPorSalaoAsync(idSalao);

                if (config == null)
                {
                    ErroMensagem = "Nenhuma configuração fiscal cadastrada para este salão. Acesse 'Configurações Fiscais' primeiro.";
                    return Page();
                }

                CnpjConfigurado = config.Cnpj ?? "(não informado)";
                RazaoSocial = config.RazaoSocial ?? "(não informado)";
                AmbienteConfigurado = config.Ambiente;
                ValidadeArmazenada = config.CertificadoValidade;

                if (config.CertificadoPfx == null || config.CertificadoPfx.Length == 0)
                {
                    ErroMensagem = "Nenhum arquivo de certificado digital (.pfx) foi enviado nas Configurações Fiscais.";
                    return Page();
                }

                if (config.CertificadoSenha == null || config.CertificadoSenha.Length == 0)
                {
                    ErroMensagem = "A senha do certificado digital não foi configurada.";
                    return Page();
                }

                // Tenta instanciar o certificado pelo mesmo pipeline da aplicação
                X509Certificate2 cert;
                try
                {
                    cert = _certificadoFactory.InstanciarCertificado(config);
                }
                catch (Exception exCert)
                {
                    ErroMensagem = $"Falha ao carregar o certificado: {exCert.Message}. Verifique se a senha está correta e o arquivo .pfx não está corrompido.";
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
                    StatusCertificado = "⏳ AINDA NÃO VÁLIDO (data de início no futuro)";
                    StatusCss = "warning";
                    CertificadoValido = false;
                }
                else if (DateTime.Now > cert.NotAfter)
                {
                    StatusCertificado = "❌ VENCIDO";
                    StatusCss = "danger";
                    CertificadoValido = false;
                }
                else if (DiasRestantes <= 30)
                {
                    StatusCertificado = $"⚠️ VÁLIDO, mas VENCE EM {DiasRestantes} DIAS";
                    StatusCss = "warning";
                    CertificadoValido = true;
                }
                else
                {
                    StatusCertificado = $"✅ VÁLIDO ({DiasRestantes} dias restantes)";
                    StatusCss = "success";
                    CertificadoValido = true;
                }

                if (!cert.HasPrivateKey)
                {
                    StatusCertificado += " | ❌ SEM CHAVE PRIVADA (assinatura XML falhará)";
                    StatusCss = "danger";
                    CertificadoValido = false;
                }

                cert.Dispose();
            }
            catch (Exception ex)
            {
                ErroMensagem = $"Erro inesperado durante o diagnóstico: {ex.Message}";
            }

            return Page();
        }
    }
}
