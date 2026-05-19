using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages;

[Authorize(Policy = "UsuarioPolicy")]
public class ImoveisModel : PageModel
{
    private readonly ImovelHandler _imovelHandler;

    public PagedResult<Imovel> Imoveis { get; set; } = new();
    public string? q { get; set; }
    public string? Status { get; set; }
    public string? Finalidade { get; set; }
    public string? TipoImovel { get; set; }
    public bool incluirInativos { get; set; }
    public int p { get; set; } = 1;
    public int IdSalaoAtual { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string MensagemTipo { get; set; } = "info";

    public IReadOnlyList<string> StatusOptions { get; } = new[] { "Ativo", "Inativo", "Vendido", "Alugado", "Rascunho" };
    public IReadOnlyList<string> FinalidadeOptions { get; } = new[] { "Venda", "Aluguel", "Temporada" };
    public IReadOnlyList<string> TipoOptions { get; } = new[] { "Casa", "Apartamento", "Lote", "Sala", "Cobertura", "Terreno", "Comercial", "Rural", "Outro" };

    public ImoveisModel(ImovelHandler imovelHandler)
    {
        _imovelHandler = imovelHandler;
    }

    public void OnGet(string? q = null, string? status = null, string? finalidade = null, string? tipoImovel = null, bool incluirInativos = false, int p = 1)
    {
        CarregarLista(q, status, finalidade, tipoImovel, incluirInativos, p);
    }

    public IActionResult OnPost(int id, string action, string? q, string? status, string? finalidade, string? tipoImovel, bool incluirInativos, int p = 1)
    {
        if (action == "alterar")
        {
            if (id <= 0)
            {
                TempData["ImoveisMensagem"] = "Nao foi possivel identificar o imovel selecionado.";
                TempData["ImoveisMensagemTipo"] = "danger";
                return RedirectToPage(new { q, status, finalidade, tipoImovel, incluirInativos, p });
            }

            return RedirectToPage("/ImovelCadastro", new { id });
        }

        if (!TryObterIdSalao(out var idSalao))
        {
            TempData["ImoveisMensagem"] = "Nao foi possivel identificar a empresa atual.";
            TempData["ImoveisMensagemTipo"] = "danger";
            return RedirectToPage(new { q, status, finalidade, tipoImovel, incluirInativos, p });
        }

        try
        {
            if (action == "inativar")
            {
                _imovelHandler.Inativar(id, idSalao);
                TempData["ImoveisMensagem"] = "Imovel inativado com sucesso.";
                TempData["ImoveisMensagemTipo"] = "success";
            }
            else if (action == "excluir")
            {
                _imovelHandler.Excluir(id, idSalao);
                TempData["ImoveisMensagem"] = "Imovel excluido com sucesso.";
                TempData["ImoveisMensagemTipo"] = "success";
            }
        }
        catch (Exception ex)
        {
            TempData["ImoveisMensagem"] = $"Nao foi possivel executar a acao: {ex.Message}";
            TempData["ImoveisMensagemTipo"] = "danger";
        }

        return RedirectToPage(new { q, status, finalidade, tipoImovel, incluirInativos, p });
    }

    private void CarregarLista(string? q, string? status, string? finalidade, string? tipoImovel, bool incluirInativos, int p)
    {
        this.q = q;
        Status = status;
        Finalidade = finalidade;
        TipoImovel = tipoImovel;
        this.incluirInativos = incluirInativos;
        this.p = p < 1 ? 1 : p;

        if (TempData.TryGetValue("ImoveisMensagem", out var mensagem) && mensagem is not null)
        {
            Mensagem = mensagem.ToString() ?? string.Empty;
            MensagemTipo = TempData.TryGetValue("ImoveisMensagemTipo", out var tipo) && tipo is not null
                ? tipo.ToString() ?? "success"
                : "success";
        }

        if (!TryObterIdSalao(out var idSalao))
        {
            Mensagem = "Nao foi possivel identificar a empresa atual.";
            MensagemTipo = "danger";
            Imoveis = new PagedResult<Imovel> { PageIndex = this.p, PageSize = 10 };
            return;
        }

        IdSalaoAtual = idSalao;

        try
        {
            Imoveis = _imovelHandler.ListarPaginadoPorSalao(idSalao, q, status, finalidade, tipoImovel, incluirInativos, this.p, 10);
        }
        catch (Exception ex)
        {
            Mensagem = $"Nao foi possivel carregar os imoveis. Verifique se o script de criacao das tabelas foi executado. Detalhe: {ex.Message}";
            MensagemTipo = "danger";
            Imoveis = new PagedResult<Imovel> { PageIndex = this.p, PageSize = 10 };
        }
    }

    private bool TryObterIdSalao(out int idSalao)
    {
        return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
    }
}
