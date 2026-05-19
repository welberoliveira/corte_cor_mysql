using CorteCor.Handlers;
using CorteCor.Models;

namespace CorteCor.Services;

public class CompraService
{
    private readonly CompraHandler _compraHandler;
    private readonly ProdutoHandler _produtoHandler;
    private readonly PessoaHandler _pessoaHandler;
    private readonly FinanceiroService _financeiroService;

    public CompraService(
        CompraHandler compraHandler,
        ProdutoHandler produtoHandler,
        PessoaHandler pessoaHandler,
        FinanceiroService financeiroService)
    {
        _compraHandler = compraHandler;
        _produtoHandler = produtoHandler;
        _pessoaHandler = pessoaHandler;
        _financeiroService = financeiroService;
    }

    public Task<PagedResult<Compra>> ListarComprasAsync(int idSalao, CompraFiltro filtro) =>
        _compraHandler.ListarComprasAsync(idSalao, filtro);

    public async Task<CompraCancelamentoResult> CancelarCompraAsync(int idSalao, int idCompra, string? usuario, string? justificativa)
    {
        if (idCompra <= 0)
        {
            throw new InvalidOperationException("Selecione uma compra valida para cancelar.");
        }

        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length < 5)
        {
            throw new InvalidOperationException("Informe uma justificativa para cancelar a compra e ajustar o estoque.");
        }

        return await _compraHandler.CancelarCompraAsync(idSalao, idCompra, usuario, justificativa);
    }

    public async Task<int> RegistrarCompraAsync(int idSalao, CompraInput input, string? usuario)
    {
        if (input.Itens == null || input.Itens.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um produto para registrar a compra.");
        }

        if (!input.IdPlano.HasValue || input.IdPlano <= 0)
        {
            throw new InvalidOperationException("Selecione uma conta analitica do plano de contas para a compra.");
        }

        if (input.PagaNaHora && (!input.IdConta.HasValue || input.IdConta <= 0))
        {
            throw new InvalidOperationException("Selecione de qual conta caixa saiu o dinheiro da compra.");
        }

        if (input.IdPessoaFornecedor.HasValue && input.IdPessoaFornecedor > 0)
        {
            var fornecedor = _pessoaHandler.ObterPorId(input.IdPessoaFornecedor.Value);
            if (fornecedor == null || fornecedor.IdSalao != idSalao || fornecedor.Excluido)
            {
                throw new InvalidOperationException("Fornecedor invalido para esta empresa.");
            }
        }

        var itens = new List<CompraItem>();
        var movimentos = new List<MovimentoEstoque>();
        var saldoProdutos = new Dictionary<int, decimal>();
        foreach (var item in input.Itens)
        {
            if (item.IdProduto <= 0 || item.Quantidade <= 0 || item.ValorUnitario < 0)
            {
                throw new InvalidOperationException("Informe produto, quantidade e valor validos para todos os itens da compra.");
            }

            if (item.Quantidade != Math.Truncate(item.Quantidade))
            {
                throw new InvalidOperationException("A quantidade de produtos na compra deve ser inteira.");
            }

            var produto = _produtoHandler.ObterPorIdESalao(item.IdProduto, idSalao)
                ?? throw new InvalidOperationException("Produto da compra nao encontrado para esta empresa.");
            if (!produto.ControlarEstoque)
            {
                throw new InvalidOperationException($"O produto '{produto.Nome}' nao esta configurado para controle de estoque.");
            }

            var saldoAnterior = saldoProdutos.TryGetValue(produto.IdProduto, out var saldoAtual)
                ? saldoAtual
                : produto.EstoqueAtual ?? 0m;
            var saldoPosterior = saldoAnterior + item.Quantidade;
            saldoProdutos[produto.IdProduto] = saldoPosterior;
            var valorTotal = decimal.Round(item.Quantidade * item.ValorUnitario, 2);
            itens.Add(new CompraItem
            {
                IdSalao = idSalao,
                IdProduto = produto.IdProduto,
                NomeProduto = produto.Nome,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
                ValorTotal = valorTotal
            });

            movimentos.Add(new MovimentoEstoque
            {
                IdSalao = idSalao,
                IdProduto = produto.IdProduto,
                TipoMovimento = MovimentoEstoqueTipo.Entrada,
                Origem = MovimentoEstoqueOrigem.Compra,
                Quantidade = item.Quantidade,
                SaldoAnterior = saldoAnterior,
                SaldoPosterior = saldoPosterior,
                Observacao = $"Entrada pela compra de produto {produto.Nome}.",
                UsuarioOperador = usuario,
                DataMovimento = input.DataCompra == default ? DateTime.Now : input.DataCompra
            });
        }

        var valorTotalCompra = itens.Sum(item => item.ValorTotal);
        var compra = new Compra
        {
            IdSalao = idSalao,
            IdPessoaFornecedor = input.IdPessoaFornecedor,
            IdPlano = input.IdPlano,
            IdConta = input.IdConta,
            Status = CompraStatus.Lancada,
            Recorrencia = RecorrenciaTipo.Nenhuma,
            PagaNaHora = input.PagaNaHora,
            ValorTotal = valorTotalCompra,
            Documento = input.Documento?.Trim(),
            Observacoes = input.Observacoes?.Trim(),
            UsuarioOperador = usuario,
            DataCompra = input.DataCompra == default ? DateTime.Now : input.DataCompra,
            DataVencimento = input.DataVencimento == default ? DateTime.Today : input.DataVencimento.Date
        };

        var idCompra = await _compraHandler.RegistrarCompraAsync(compra, itens, movimentos);
        var idTitulo = await _financeiroService.SalvarTituloAsync(idSalao, new FinanceiroTitulo
        {
            Tipo = FinanceiroTipoTitulo.Pagar,
            Origem = FinanceiroOrigemTitulo.Compra,
            IdPessoa = input.IdPessoaFornecedor,
            IdPlano = input.IdPlano,
            IdConta = input.IdConta,
            Descricao = $"Compra {idCompra}",
            Documento = string.IsNullOrWhiteSpace(input.Documento) ? $"CP-{idCompra}" : input.Documento.Trim(),
            Status = input.PagaNaHora ? FinanceiroStatusTitulo.Liquidado : FinanceiroStatusTitulo.Aberto,
            ValorOriginal = valorTotalCompra,
            ValorLiquidado = input.PagaNaHora ? valorTotalCompra : 0m,
            ValorAberto = input.PagaNaHora ? 0m : valorTotalCompra,
            DataCompetencia = compra.DataCompra.Date,
            DataVencimento = compra.DataVencimento,
            DataLiquidacao = input.PagaNaHora ? compra.DataCompra : null,
            Conciliado = input.PagaNaHora,
            Observacoes = $"Gerado automaticamente pela compra {idCompra}."
        });

        await _compraHandler.AtualizarTituloCompraAsync(idSalao, idCompra, idTitulo);
        return idCompra;
    }
}
