using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace CorteCor.Services;

public class SuporteService
{
    private readonly SuporteHandler _suporteHandler;
    private readonly BrevoEmailService _emailService;
    private readonly IConfiguration _configuration;

    public SuporteService(SuporteHandler suporteHandler, BrevoEmailService emailService, IConfiguration configuration)
    {
        _suporteHandler = suporteHandler;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Guid> RegistrarChamadoAsync(SuporteChamado chamado)
    {
        chamado.IdChamado = chamado.IdChamado == Guid.Empty ? Guid.NewGuid() : chamado.IdChamado;
        chamado.Mensagem = chamado.Mensagem.Trim();
        chamado.Status = SuporteChamadoStatus.Solicitado;
        await _suporteHandler.RegistrarAsync(chamado);

        var emailSuporte = _configuration["Suporte:EmailDestino"];
        if (string.IsNullOrWhiteSpace(emailSuporte))
        {
            return chamado.IdChamado;
        }

        var corpo = $"""
<p><strong>Novo chamado de suporte</strong></p>
<p><strong>Código:</strong> {WebUtility.HtmlEncode(chamado.IdChamado.ToString())}</p>
<p><strong>Empresa/Salao:</strong> {chamado.IdSalao}</p>
<p><strong>Usuário:</strong> {WebUtility.HtmlEncode(chamado.NomeUsuario ?? "-")} ({WebUtility.HtmlEncode(chamado.EmailUsuario ?? "-")})</p>
<p><strong>Origem:</strong> {WebUtility.HtmlEncode(chamado.UrlOrigem ?? "-")}</p>
<p><strong>Mensagem:</strong></p>
<pre>{WebUtility.HtmlEncode(chamado.Mensagem)}</pre>
""";

        var (ok, erro) = await _emailService.EnviarEmailGenericoAsync(emailSuporte, "Suporte Tonni", $"Chamado de suporte {chamado.IdChamado}", corpo);
        if (!ok)
        {
            await _suporteHandler.AtualizarStatusAsync(
                chamado.IdSalao,
                chamado.IdChamado,
                SuporteChamadoStatus.Solicitado,
                erro);
        }

        return chamado.IdChamado;
    }

    public Task<PagedResult<SuporteChamado>> ListarChamadosAsync(int idSalao, SuporteChamadoFiltro filtro) =>
        _suporteHandler.ListarAsync(idSalao, filtro);

    public async Task AtualizarStatusChamadoAsync(int idSalao, Guid idChamado, string status)
    {
        var statusNormalizado = SuporteChamadoStatus.Todos.FirstOrDefault(item =>
            string.Equals(item, status?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(statusNormalizado))
        {
            throw new InvalidOperationException("Status de chamado inválido.");
        }

        await _suporteHandler.AtualizarStatusAsync(idSalao, idChamado, statusNormalizado, null);
    }
}
