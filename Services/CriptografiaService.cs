using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CorteCor.Services
{
    public class CriptografiaService : ICriptografiaService
    {
        private readonly string _chaveMestre;

        public CriptografiaService(IConfiguration configuration)
        {
            // Lê a chave mestra de criptografia do appsettings.json ou variáveis de ambiente.
            // A chave deve ter 32 bytes (256 bits) para o AES-256.
            _chaveMestre = configuration["FiscalSettings:MasterKey"] 
                           ?? "F3B849CA80B5C1AEEB6D1B48E9E397ED"; // Default para dev fallback
            
            if (_chaveMestre.Length != 32)
            {
                // Pad ou Truncate para garantir 32 caracteres (256 bits)
                _chaveMestre = _chaveMestre.PadRight(32, '0').Substring(0, 32);
            }
        }

        public byte[] Criptografar(string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano)) return null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(_chaveMestre);
                aesAlg.GenerateIV(); // O IV é gerado randomicamente

                using (var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
                using (var msEncrypt = new MemoryStream())
                {
                    // Prepende o IV ao array resultante (os primeiros 16 bytes)
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(textoPlano);
                    }
                    
                    return msEncrypt.ToArray();
                }
            }
        }

        public string Descriptografar(byte[] textoCriptografado)
        {
            if (textoCriptografado == null || textoCriptografado.Length == 0) return null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(_chaveMestre);

                // O IV são os primeiros 16 bytes.
                byte[] iv = new byte[16];
                Array.Copy(textoCriptografado, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;

                using (var msDecrypt = new MemoryStream(textoCriptografado, iv.Length, textoCriptografado.Length - iv.Length))
                using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
