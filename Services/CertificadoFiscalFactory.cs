using System.Security.Cryptography.X509Certificates;
using CorteCor.Models;

namespace CorteCor.Services
{
    public class CertificadoFiscalFactory
    {
        private readonly ICriptografiaService _criptoService;

        public CertificadoFiscalFactory(ICriptografiaService criptoService)
        {
            _criptoService = criptoService;
        }

        public X509Certificate2 InstanciarCertificado(SalaoConfigFiscal config)
        {
            if (config.CertificadoPfx == null || config.CertificadoSenha == null)
            {
                throw new InvalidOperationException("Certificado A1 não foi configurado para este Salão.");
            }

            // Descriptografa a senha armazenada no banco usando a chave mestre da infraestrutura
            string senhaOriginal = _criptoService.Descriptografar(config.CertificadoSenha);

            // EphemeralKeySet e MachineKeySet são IMPRESCINDÍVEIS para não dar crash em ambiente Web/IIS/Container
            return new X509Certificate2(
                config.CertificadoPfx, 
                senhaOriginal, 
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable
            );
        }
    }
}
