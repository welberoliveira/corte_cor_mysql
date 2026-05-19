using System.Security.Claims;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Api;

[Authorize(Policy = "UsuarioPolicy")]
public class ClientesModel : PageModel
{
    private readonly PessoaHandler _pessoaHandler;

    public ClientesModel(PessoaHandler pessoaHandler)
    {
        _pessoaHandler = pessoaHandler;
    }

    public async Task<IActionResult> OnGetAsync(string? q, bool somenteClientes = true, bool somenteFornecedores = false)
    {
        var pessoas = await _pessoaHandler.BuscarParaSelecaoAsync(ObterIdSalao(), q, somenteClientes, somenteFornecedores);
        return new JsonResult(pessoas.Select(pessoa => new
        {
            id = pessoa.IdPessoa,
            nome = pessoa.Nome,
            documento = pessoa.CpfCnpj,
            email = pessoa.Email,
            telefone = pessoa.Telefone,
            rotulo = MontarRotulo(pessoa.Nome, pessoa.CpfCnpj, pessoa.Email, pessoa.Telefone)
        }));
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private static string MontarRotulo(string? nome, string? documento, string? email, string? telefone)
    {
        var detalhes = new[] { documento, email, telefone }
            .Where(valor => !string.IsNullOrWhiteSpace(valor))
            .Select(valor => valor!.Trim())
            .ToList();

        return detalhes.Count == 0
            ? nome?.Trim() ?? string.Empty
            : $"{nome?.Trim()} - {string.Join(" | ", detalhes)}";
    }
}
