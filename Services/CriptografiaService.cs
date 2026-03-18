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
            _chaveMestre = configuration["FiscalSettings:MasterKey"];

            if (string.IsNullOrWhiteSpace(_chaveMestre) ||
                _chaveMestre.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            {
                _chaveMestre = "F3B849CA80B5C1AEEB6D1B48E9E397ED";
            }

            if (_chaveMestre.Length != 32)
            {
                _chaveMestre = _chaveMestre.PadRight(32, '0').Substring(0, 32);
            }
        }

        public byte[] Criptografar(string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano))
            {
                return null;
            }

            using var aesAlg = Aes.Create();
            aesAlg.Key = Encoding.UTF8.GetBytes(_chaveMestre);
            aesAlg.GenerateIV();

            using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using var msEncrypt = new MemoryStream();
            msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(textoPlano);
            }

            return msEncrypt.ToArray();
        }

        public string Descriptografar(byte[] textoCriptografado)
        {
            if (textoCriptografado == null || textoCriptografado.Length == 0)
            {
                return null;
            }

            using var aesAlg = Aes.Create();
            aesAlg.Key = Encoding.UTF8.GetBytes(_chaveMestre);

            var iv = new byte[16];
            Array.Copy(textoCriptografado, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;

            using var msDecrypt = new MemoryStream(textoCriptografado, iv.Length, textoCriptografado.Length - iv.Length);
            using var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
    }
}
