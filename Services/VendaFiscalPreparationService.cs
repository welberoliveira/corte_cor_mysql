using CorteCor.Handlers;
using CorteCor.Models;

namespace CorteCor.Services;

public class VendaFiscalPreparationService
{
    private readonly VendaEstoqueHandler _vendaEstoqueHandler;
    private readonly PessoaHandler _pessoaHandler;
    private readonly NotaFiscalHandler _notaFiscalHandler;
    private readonly NotaFiscalAvulsaService _notaFiscalAvulsaService;

    public VendaFiscalPreparationService(
        VendaEstoqueHandler vendaEstoqueHandler,
        PessoaHandler pessoaHandler,
        NotaFiscalHandler notaFiscalHandler,
        NotaFiscalAvulsaService notaFiscalAvulsaService)
    {
        _vendaEstoqueHandler = vendaEstoqueHandler;
        _pessoaHandler = pessoaHandler;
        _notaFiscalHandler = notaFiscalHandler;
        _notaFiscalAvulsaService = notaFiscalAvulsaService;
    }

    public async Task<bool> PossuiServicoFaturavelAsync(int idSalao, int idVendaProduto)
    {
        var itens = await _vendaEstoqueHandler.ListarItensVendaAsync(idSalao, idVendaProduto);
        return itens.Any(i => string.Equals(i.TipoItem, VendaProdutoTipoItem.Servico, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<NotaFiscalOperacaoResult> EmitirNotaServicoAsync(int idSalao, int idVendaProduto, string? usuario = null)
    {
        var venda = await _vendaEstoqueHandler.ObterVendaAsync(idSalao, idVendaProduto)
            ?? throw new InvalidOperationException("Venda não encontrada.");

        if (string.Equals(venda.Status, VendaProdutoStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Vendas canceladas não podem emitir nota fiscal.");
        }

        var notaAtiva = await _notaFiscalHandler.ObterNotaAtivaPorVendaAsync(idSalao, idVendaProduto);
        if (notaAtiva != null)
        {
            throw new InvalidOperationException($"A venda já possui {notaAtiva.TipoNota} ativa vinculada.");
        }

        var itens = await _vendaEstoqueHandler.ListarItensVendaAsync(idSalao, idVendaProduto);
        var itensServico = itens
            .Where(i => string.Equals(i.TipoItem, VendaProdutoTipoItem.Servico, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!itensServico.Any())
        {
            throw new InvalidOperationException("Esta venda não possui serviços para emissão de nota fiscal.");
        }

        var cliente = venda.IdPessoa.HasValue ? _pessoaHandler.ObterPorId(venda.IdPessoa.Value) : null;
        if (cliente == null)
        {
            throw new InvalidOperationException("Selecione um cliente válido na venda antes de emitir a nota fiscal.");
        }

        var contexto = await _notaFiscalAvulsaService.ObterContextoTelaAsync(idSalao, "NFSE", 0, 0, 0);
        var codigoTributacao = itensServico
            .Select(i => NormalizarSomenteDigitos(i.CodigoTributacaoMunicipio))
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
            ?? "010401";
        var aliquotaIss = itensServico
            .Select(i => i.AliquotaIss)
            .FirstOrDefault(v => v.HasValue && v.Value > 0) ?? 5m;

        var descricao = itensServico.Count == 1
            ? itensServico[0].Descricao
            : $"Serviços da venda {idVendaProduto}: {string.Join(", ", itensServico.Select(i => i.Descricao).Take(4))}";

        var request = new NotaFiscalAvulsaRequest
        {
            Ambiente = contexto.Ambiente,
            Modelo = "NFSE",
            NaturezaOperacao = "Prestacao de servico",
            Serie = contexto.Serie,
            Numero = contexto.NumeroSugerido,
            DataEmissao = venda.DataVenda,
            EmitenteCnpj = contexto.EmitenteCnpj,
            EmitenteNome = contexto.EmitenteNome,
            EmitenteIE = contexto.EmitenteIE,
            EmitenteIM = contexto.EmitenteIM,
            EmitenteCRT = contexto.EmitenteCRT,
            EmitenteLogradouro = contexto.EmitenteLogradouro,
            EmitenteNumero = contexto.EmitenteNumero,
            EmitenteBairro = contexto.EmitenteBairro,
            EmitenteCep = contexto.EmitenteCep,
            EmitenteCidade = contexto.EmitenteCidade,
            EmitenteUF = contexto.EmitenteUF,
            EmitenteCodMun = contexto.EmitenteCodMun,
            DestinatarioCpfCnpj = cliente.CpfCnpj,
            DestinatarioNome = cliente.Nome,
            DestinatarioLogradouro = cliente.Logradouro,
            DestinatarioNumero = cliente.Numero,
            DestinatarioBairro = cliente.Bairro,
            DestinatarioCep = cliente.Cep,
            DestinatarioCidade = cliente.Cidade,
            DestinatarioUF = string.IsNullOrWhiteSpace(cliente.UF) ? "MG" : cliente.UF,
            DestinatarioCodMun = InferirCodigoMunicipio(cliente.Cidade, cliente.UF, cliente.Cep),
            DestinatarioEmail = cliente.Email,
            Itens = new List<NotaFiscalAvulsaItemRequest>
            {
                new()
                {
                    CProd = $"VS{idVendaProduto}",
                    XProd = descricao,
                    NCM = "00",
                    CFOP = "5933",
                    UCom = "UN",
                    QCom = 1,
                    VUnCom = itensServico.Sum(i => i.ValorTotal),
                    VProd = itensServico.Sum(i => i.ValorTotal),
                    CodigoTributacao = codigoTributacao,
                    AliquotaISS = aliquotaIss
                }
            }
        };

        var resultado = await _notaFiscalAvulsaService.EmitirAsync(idSalao, request, usuario);
        if (resultado.NotaFiscal != null)
        {
            resultado.NotaFiscal.IdVendaProduto = idVendaProduto;
            await _notaFiscalHandler.UpdateAsync(resultado.NotaFiscal);
            resultado.IdNotaFiscal = resultado.NotaFiscal.IdNotaFiscal;
        }

        return resultado;
    }

    private static int InferirCodigoMunicipio(string? cidade, string? uf, string? cep)
    {
        if (!string.IsNullOrWhiteSpace(cidade) &&
            cidade.Trim().Equals("Montes Claros", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(uf) || uf.Trim().Equals("MG", StringComparison.OrdinalIgnoreCase)))
        {
            return 3143302;
        }

        if (!string.IsNullOrWhiteSpace(cep) && cep.Replace("-", "").StartsWith("394", StringComparison.Ordinal))
        {
            return 3143302;
        }

        return 3143302;
    }

    private static string? NormalizarSomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var digits = new string(valor.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}
