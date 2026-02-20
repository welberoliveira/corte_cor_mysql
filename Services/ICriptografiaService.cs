namespace CorteCor.Services
{
    public interface ICriptografiaService
    {
        byte[] Criptografar(string textoPlano);
        string Descriptografar(byte[] textoCriptografado);
    }
}
