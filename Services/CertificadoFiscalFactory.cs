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

            // Simplificando o Storage Flags para o padrão mínimo aceitável pelo HttpClient no Windows
            // Sem o MachineKeySet ou PersistKeySet, a chave vai interinamente para o provedor de Criptografia em Memória 
            // e é enviada diretamente no handshake TLS sem problemas de acesso a pastas de sistema.
            return new X509Certificate2(
                config.CertificadoPfx, 
                senhaOriginal, 
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet
            );
        }
    }
}
