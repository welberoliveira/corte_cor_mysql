namespace CorteCor.Services
{
    public interface IWhatsappService
    {
        Task<(bool Success, string? ErrorMessage)> EnviarMensagemAsync(string telefone, string mensagem);
    }
}
