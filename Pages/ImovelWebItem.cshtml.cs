using System.Net;
using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages;

[AllowAnonymous]
public class ImovelWebItemModel : PageModel
{
    private readonly ImovelHandler _imovelHandler;
    private readonly ILogger<ImovelWebItemModel> _logger;

    public Imovel? Imovel { get; private set; }
    public string Mensagem { get; private set; } = string.Empty;
    public string MensagemTipo { get; private set; } = "success";

    [BindProperty]
    public ImovelLead Lead { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? IdSalao { get; set; }

    public ImovelWebItemModel(ImovelHandler imovelHandler, ILogger<ImovelWebItemModel> logger)
    {
        _imovelHandler = imovelHandler;
        _logger = logger;
    }

    public IActionResult OnGet(int id)
    {
        CarregarImovel(id);
        return Page();
    }

    public IActionResult OnPostContato(int id)
    {
        if (!CarregarImovel(id) || Imovel == null)
        {
            return Page();
        }

        if (!ValidarLead())
        {
            MensagemTipo = "danger";
            return Page();
        }

        try
        {
            Lead.IdImovel = Imovel.IdImovel;
            Lead.Status = "Novo";
            Lead.Origem = "Imoveis Web";
            Lead.IpOrigem = HttpContext.Connection.RemoteIpAddress?.ToString();
            Lead.UserAgent = Request.Headers.UserAgent.ToString();
            _imovelHandler.AdicionarLead(Lead);
            Mensagem = "Contato enviado com sucesso. O lead foi registrado para este imovel.";
            MensagemTipo = "success";
            Lead = new ImovelLead();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar lead do imovel {IdImovel}", id);
            Mensagem = "Nao foi possivel registrar o contato agora. Tente novamente em instantes.";
            MensagemTipo = "danger";
        }

        CarregarImovel(id);
        return Page();
    }

    public IReadOnlyList<ImovelFoto> FotosOrdenadas()
    {
        return Imovel?.Fotos.OrderByDescending(f => f.FotoCapa).ThenBy(f => f.Ordem).ThenBy(f => f.IdFoto).ToList()
            ?? new List<ImovelFoto>();
    }

    public string FotoPrincipal()
    {
        return FotosOrdenadas().FirstOrDefault()?.CaminhoArquivo
            ?? (!string.IsNullOrWhiteSpace(Imovel?.FotoCapaUrl) ? Imovel.FotoCapaUrl : "/img/cortecor.png");
    }

    public string FormatarValor()
    {
        if (Imovel == null || Imovel.PrecoSobConsulta)
        {
            return "Sob consulta";
        }

        var valor = string.Equals(Imovel.Finalidade, "Aluguel", StringComparison.OrdinalIgnoreCase)
            ? Imovel.ValorAluguel
            : Imovel.ValorVenda;

        return valor.HasValue && valor > 0
            ? $"R$ {valor.Value:N2}"
            : "Sob consulta";
    }

    public string EnderecoPublico()
    {
        if (Imovel == null)
        {
            return string.Empty;
        }

        if (!Imovel.ExibirEnderecoCompleto)
        {
            return $"{Imovel.Bairro}, {Imovel.Cidade} / {Imovel.Estado}";
        }

        var numero = string.IsNullOrWhiteSpace(Imovel.Numero) ? "s/n" : Imovel.Numero;
        var complemento = string.IsNullOrWhiteSpace(Imovel.Complemento) ? string.Empty : $" - {Imovel.Complemento}";
        return $"{Imovel.Logradouro}, {numero}{complemento} - {Imovel.Bairro}, {Imovel.Cidade} / {Imovel.Estado}";
    }

    public string WhatsAppUrl()
    {
        if (Imovel == null)
        {
            return "#";
        }

        var digits = new string(Imovel.WhatsApp.Where(char.IsDigit).ToArray());
        if (!digits.StartsWith("55", StringComparison.Ordinal) && digits.Length >= 10)
        {
            digits = $"55{digits}";
        }

        var mensagem = (Imovel.MensagemPadraoWhatsApp ?? string.Empty)
            .Replace("{codigo}", Imovel.CodigoImovel, StringComparison.OrdinalIgnoreCase);
        return $"https://wa.me/{digits}?text={WebUtility.UrlEncode(mensagem)}";
    }

    public IReadOnlyList<string> Caracteristicas()
    {
        var itens = new List<string>();
        AddNumber(itens, Imovel?.AreaConstruidaPrivativa, "m2 privativos");
        AddNumber(itens, Imovel?.AreaLoteTerreno, "m2 de lote");
        AddInt(itens, Imovel?.Quartos, "quarto", "quartos");
        AddInt(itens, Imovel?.Suites, "suite", "suites");
        AddInt(itens, Imovel?.Banheiros, "banheiro", "banheiros");
        AddInt(itens, Imovel?.Lavabos, "lavabo", "lavabos");
        AddInt(itens, Imovel?.VagasGaragem, "vaga", "vagas");
        AddInt(itens, Imovel?.Salas, "sala", "salas");
        AddFlag(itens, Imovel?.Piscina, "Piscina");
        AddFlag(itens, Imovel?.ArCondicionado, "Ar-condicionado");
        AddFlag(itens, Imovel?.Churrasqueira, "Churrasqueira");
        AddFlag(itens, Imovel?.Sauna, "Sauna");
        AddFlag(itens, Imovel?.Jardim, "Jardim");
        AddFlag(itens, Imovel?.AreaGourmet, "Area gourmet");
        AddFlag(itens, Imovel?.Jacuzzi, "Jacuzzi");
        AddFlag(itens, Imovel?.Hidromassagem, "Hidromassagem");
        AddFlag(itens, Imovel?.Escritorio, "Escritorio");
        AddFlag(itens, Imovel?.SalaTV, "Sala de TV");
        AddFlag(itens, Imovel?.CozinhaPlanejada, "Cozinha planejada");
        AddFlag(itens, Imovel?.ClosetCaracteristica, "Closet");
        AddFlag(itens, Imovel?.VarandaCaracteristica, "Varanda");
        AddFlag(itens, Imovel?.LavaboCaracteristica, "Lavabo");
        return itens;
    }

    private bool CarregarImovel(int id)
    {
        try
        {
            var idSalao = TryObterIdSalao(out var resolvedIdSalao) ? resolvedIdSalao : (int?)null;
            Imovel = _imovelHandler.ObterWebPorId(id, idSalao);
            if (Imovel == null)
            {
                Mensagem = "Imovel nao encontrado.";
                MensagemTipo = "warning";
                return false;
            }

            IdSalao = Imovel.IdSalao;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar imovel web {IdImovel}", id);
            Mensagem = "Nao foi possivel carregar este imovel.";
            MensagemTipo = "danger";
            return false;
        }
    }

    private bool ValidarLead()
    {
        if (string.IsNullOrWhiteSpace(Lead.NomeInteressado) ||
            string.IsNullOrWhiteSpace(Lead.Email) ||
            string.IsNullOrWhiteSpace(Lead.TelefoneWhatsapp))
        {
            Mensagem = "Preencha nome, e-mail e telefone/WhatsApp para enviar o contato.";
            return false;
        }

        if (!Lead.AceiteTermos)
        {
            Mensagem = "Confirme o aceite dos termos para enviar o contato.";
            return false;
        }

        return true;
    }

    private bool TryObterIdSalao(out int idSalao)
    {
        if (IdSalao.HasValue && IdSalao.Value > 0)
        {
            idSalao = IdSalao.Value;
            return true;
        }

        return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
    }

    private static void AddNumber(List<string> itens, decimal? value, string suffix)
    {
        if (value.HasValue && value > 0)
        {
            itens.Add($"{value.Value:N0} {suffix}");
        }
    }

    private static void AddInt(List<string> itens, int? value, string singular, string plural)
    {
        if (value.HasValue && value > 0)
        {
            itens.Add($"{value.Value} {(value.Value == 1 ? singular : plural)}");
        }
    }

    private static void AddFlag(List<string> itens, bool? value, string label)
    {
        if (value == true)
        {
            itens.Add(label);
        }
    }
}
