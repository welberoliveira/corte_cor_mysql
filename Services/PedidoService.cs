using CorteCor.Handlers;
using CorteCor.Models;

namespace CorteCor.Services;

public class PedidoService
{
    private readonly PedidoHandler _pedidoHandler;
    private readonly PessoaHandler _pessoaHandler;
    private readonly ProdutoHandler _produtoHandler;
    private readonly ServicoHandler _servicoHandler;
    private readonly MeioPagamentoHandler _meioPagamentoHandler;
    private readonly VendaService _vendaService;
    private readonly CrmService _crmService;

    public PedidoService(
        PedidoHandler pedidoHandler,
        PessoaHandler pessoaHandler,
        ProdutoHandler produtoHandler,
        ServicoHandler servicoHandler,
        MeioPagamentoHandler meioPagamentoHandler,
        VendaService vendaService,
        CrmService crmService)
    {
        _pedidoHandler = pedidoHandler;
        _pessoaHandler = pessoaHandler;
        _produtoHandler = produtoHandler;
        _servicoHandler = servicoHandler;
        _meioPagamentoHandler = meioPagamentoHandler;
        _vendaService = vendaService;
        _crmService = crmService;
    }

    public async Task<PedidoContexto> ObterContextoAsync(int idSalao, PedidoFiltro filtro)
    {
        await _pedidoHandler.AtualizarPedidosVencidosAsync(idSalao);

        var clientes = _pessoaHandler.ListarPorSalao(idSalao)?.Where(p => p.IsCliente && !p.Excluido).OrderBy(p => p.Nome).ToList() ?? new List<Pessoa>();
        var produtos = _produtoHandler.ListarPorSalao(idSalao)?.Where(p => !p.Arquivado).OrderBy(p => p.Nome).ToList() ?? new List<Produto>();
        var servicos = _servicoHandler.ListarPorSalao(idSalao)?.Where(s => !s.Arquivado).OrderBy(s => s.Nome).ToList() ?? new List<Servico>();
        var meios = _meioPagamentoHandler.ListarPorSalao(idSalao, true)?.OrderBy(m => m.Nome).ToList() ?? new List<MeioPagamento>();
        var pedidos = await _pedidoHandler.ListarPedidosAsync(idSalao, filtro);

        foreach (var pedido in pedidos.Items)
        {
            pedido.ClasseStatusFiscal = string.IsNullOrWhiteSpace(pedido.StatusFiscal)
                ? "bg-secondary"
                : NotaFiscalAvulsaService.ObterClasseStatus(pedido.StatusFiscal);
        }

        return new PedidoContexto
        {
            Clientes = clientes,
            Produtos = produtos,
            Servicos = servicos,
            MeiosPagamento = meios,
            PedidosRecentes = pedidos
        };
    }

    public async Task<PedidoOperacaoResult> CriarPedidoAsync(int idSalao, PedidoCheckoutInput input, string? usuario)
    {
        if (!input.IdPessoa.HasValue || input.IdPessoa.Value <= 0)
        {
            throw new InvalidOperationException("Selecione um cliente para registrar o pedido.");
        }

        if (input.Itens == null || input.Itens.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um produto ou serviço ao pedido.");
        }

        if (input.ValidoAte.Date < DateTime.Today)
        {
            throw new InvalidOperationException("Informe um prazo de validade igual ou posterior a hoje.");
        }

        var cliente = _pessoaHandler.ObterPorId(input.IdPessoa.Value);
        if (cliente == null || cliente.IdSalao != idSalao)
        {
            throw new InvalidOperationException("Cliente inválido para este salão.");
        }

        var meioPagamento = input.IdMeioPagamento.HasValue && input.IdMeioPagamento > 0
            ? _meioPagamentoHandler.ObterPorId(input.IdMeioPagamento.Value)
            : null;
        if (meioPagamento != null && meioPagamento.IdSalao != idSalao)
        {
            throw new InvalidOperationException("Meio de pagamento inválido para este salão.");
        }

        var itens = NormalizarItens(idSalao, input.Itens, validarEstoque: false);
        var subtotalProdutos = itens.Where(i => i.TipoItem == VendaProdutoTipoItem.Produto).Sum(i => i.ValorTotal);
        var subtotalServicos = itens.Where(i => i.TipoItem == VendaProdutoTipoItem.Servico).Sum(i => i.ValorTotal);
        var desconto = input.Desconto < 0 ? 0m : input.Desconto;
        var acrescimo = input.Acrescimo < 0 ? 0m : input.Acrescimo;
        var valorTotal = decimal.Round(subtotalProdutos + subtotalServicos - desconto + acrescimo, 2);

        if (valorTotal <= 0)
        {
            throw new InvalidOperationException("O valor total do pedido precisa ser maior que zero.");
        }

        var pedido = new Pedido
        {
            IdSalao = idSalao,
            IdPessoa = input.IdPessoa,
            IdMeioPagamento = meioPagamento?.IdMeioPagamento,
            TipoPagamento = !string.IsNullOrWhiteSpace(input.TipoPagamento) ? input.TipoPagamento.Trim() : meioPagamento?.Nome,
            ValidoAte = input.ValidoAte.Date,
            Status = PedidoStatus.Aberto,
            SubtotalProdutos = decimal.Round(subtotalProdutos, 2),
            SubtotalServicos = decimal.Round(subtotalServicos, 2),
            Desconto = desconto,
            Acrescimo = acrescimo,
            ValorTotal = valorTotal,
            Observacoes = input.Observacoes?.Trim(),
            Origem = "Manual",
            UsuarioOperador = usuario,
            DataPedido = DateTime.Now
        };

        var idPedido = await _pedidoHandler.CriarPedidoAsync(pedido, itens.Select(MapPedidoItem).ToList());

        TentarRegistrarInteracaoCrm(idSalao, input.IdPessoa, new CrmInteracao
        {
            Canal = CrmCanal.Sistema,
            Tipo = "Pedido",
            Assunto = "Pedido criado",
            Descricao = $"Pedido #{idPedido} registrado com validade até {pedido.ValidoAte:dd/MM/yyyy}. Total de {valorTotal:N2}.",
            Referencia = $"Pedido:{idPedido}",
            OrigemSistema = true,
            DataInteracao = DateTime.Now
        });

        return new PedidoOperacaoResult
        {
            Success = true,
            IdPedido = idPedido,
            Mensagem = $"Pedido #{idPedido} criado com sucesso.",
            MensagemTipo = "success"
        };
    }

    public async Task<PedidoOperacaoResult> ConverterEmVendaAsync(int idSalao, PedidoConversaoInput input, string? usuario)
    {
        await _pedidoHandler.AtualizarPedidosVencidosAsync(idSalao);

        var pedido = await _pedidoHandler.ObterPedidoAsync(idSalao, input.IdPedido)
            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (pedido.Status == PedidoStatus.Cancelado)
        {
            throw new InvalidOperationException("Pedidos cancelados não podem ser convertidos em venda.");
        }

        if (pedido.Status == PedidoStatus.Convertido && pedido.IdVendaProduto.HasValue)
        {
            throw new InvalidOperationException($"Este pedido já foi convertido na venda #{pedido.IdVendaProduto.Value}.");
        }

        var itensPedido = await _pedidoHandler.ListarItensPedidoAsync(idSalao, input.IdPedido);
        if (!itensPedido.Any())
        {
            throw new InvalidOperationException("O pedido não possui itens para conversão.");
        }

        var vendaInput = new VendaCheckoutInput
        {
            IdPessoa = pedido.IdPessoa,
            IdMeioPagamento = input.IdMeioPagamento ?? pedido.IdMeioPagamento,
            TipoPagamento = pedido.TipoPagamento,
            RecebidoNaHora = input.RecebidoNaHora,
            EmitirNotaFiscalServico = input.EmitirNotaFiscalServico,
            Desconto = pedido.Desconto,
            Acrescimo = pedido.Acrescimo,
            Observacoes = pedido.Observacoes,
            Itens = itensPedido.Select(i => new VendaItemInput
            {
                TipoItem = i.TipoItem,
                IdProduto = i.IdProduto,
                IdServico = i.IdServico,
                Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario
            }).ToList()
        };

        var resultadoVenda = await _vendaService.FinalizarVendaAsync(
            idSalao,
            vendaInput,
            usuario,
            origem: $"Pedido:{pedido.IdPedido}",
            observacoesComplementares: $"Venda originada do pedido #{pedido.IdPedido}. {input.ObservacoesConversao}".Trim());

        if (!resultadoVenda.IdVendaProduto.HasValue)
        {
            throw new InvalidOperationException("Não foi possível identificar a venda gerada a partir do pedido.");
        }

        await _pedidoHandler.MarcarPedidoComoConvertidoAsync(
            idSalao,
            pedido.IdPedido,
            resultadoVenda.IdVendaProduto.Value,
            usuario,
            $"Pedido convertido na venda #{resultadoVenda.IdVendaProduto.Value}.");

        TentarRegistrarInteracaoCrm(idSalao, pedido.IdPessoa, new CrmInteracao
        {
            Canal = CrmCanal.Sistema,
            Tipo = "Pedido",
            Assunto = "Pedido convertido em venda",
            Descricao = $"Pedido #{pedido.IdPedido} convertido na venda #{resultadoVenda.IdVendaProduto.Value}.",
            Referencia = $"Pedido:{pedido.IdPedido}",
            OrigemSistema = true,
            DataInteracao = DateTime.Now
        });

        return new PedidoOperacaoResult
        {
            Success = true,
            IdPedido = pedido.IdPedido,
            IdVendaProduto = resultadoVenda.IdVendaProduto,
            Mensagem = input.EmitirNotaFiscalServico
                ? $"Pedido convertido com sucesso. {resultadoVenda.Mensagem}"
                : $"Pedido convertido na venda #{resultadoVenda.IdVendaProduto.Value} com sucesso.",
            MensagemTipo = resultadoVenda.MensagemTipo
        };
    }

    public async Task CancelarPedidoAsync(int idSalao, int idPedido, string? usuario, string? observacao = null)
    {
        var pedido = await _pedidoHandler.ObterPedidoAsync(idSalao, idPedido)
            ?? throw new InvalidOperationException("Pedido não encontrado.");

        if (pedido.Status == PedidoStatus.Convertido)
        {
            throw new InvalidOperationException("Pedidos já convertidos não podem ser cancelados.");
        }

        await _pedidoHandler.CancelarPedidoAsync(idSalao, idPedido, usuario, observacao ?? $"Pedido #{idPedido} cancelado.");

        TentarRegistrarInteracaoCrm(idSalao, pedido.IdPessoa, new CrmInteracao
        {
            Canal = CrmCanal.Sistema,
            Tipo = "Pedido",
            Assunto = "Pedido cancelado",
            Descricao = $"Pedido #{idPedido} cancelado. {observacao}".Trim(),
            Referencia = $"Pedido:{idPedido}",
            OrigemSistema = true,
            DataInteracao = DateTime.Now
        });
    }

    public Task<List<PedidoItem>> ListarItensPedidoAsync(int idSalao, int idPedido) =>
        _pedidoHandler.ListarItensPedidoAsync(idSalao, idPedido);

    private List<VendaProdutoItem> NormalizarItens(int idSalao, IEnumerable<VendaItemInput> itensInput, bool validarEstoque)
    {
        var itens = new List<VendaProdutoItem>();

        foreach (var item in itensInput)
        {
            if (item.Quantidade <= 0)
            {
                throw new InvalidOperationException("Todos os itens precisam ter quantidade maior que zero.");
            }

            if (string.Equals(item.TipoItem, VendaProdutoTipoItem.Produto, StringComparison.OrdinalIgnoreCase))
            {
                if (!item.IdProduto.HasValue || item.IdProduto.Value <= 0)
                {
                    throw new InvalidOperationException("Selecione um produto válido.");
                }

                var produto = _produtoHandler.ObterPorIdESalao(item.IdProduto.Value, idSalao)
                    ?? throw new InvalidOperationException("Produto do pedido não encontrado para este salão.");
                var valorUnitario = item.ValorUnitario.GetValueOrDefault(produto.PrecoVenda);
                if (valorUnitario < 0)
                {
                    throw new InvalidOperationException($"O produto '{produto.Nome}' possui valor inválido.");
                }

                if (validarEstoque && produto.ControlarEstoque && (produto.EstoqueAtual ?? 0m) < item.Quantidade)
                {
                    throw new InvalidOperationException($"Estoque insuficiente para o produto '{produto.Nome}'.");
                }

                itens.Add(new VendaProdutoItem
                {
                    TipoItem = VendaProdutoTipoItem.Produto,
                    IdProduto = produto.IdProduto,
                    Descricao = produto.Nome,
                    Quantidade = item.Quantidade,
                    ValorUnitario = valorUnitario,
                    ValorTotal = decimal.Round(item.Quantidade * valorUnitario, 2),
                    Unidade = string.IsNullOrWhiteSpace(produto.UnidadeComercial) ? "UN" : produto.UnidadeComercial!,
                    ControlaEstoque = produto.ControlarEstoque,
                    Ncm = produto.NCM,
                    Cfop = "5102"
                });
            }
            else
            {
                if (!item.IdServico.HasValue || item.IdServico.Value <= 0)
                {
                    throw new InvalidOperationException("Selecione um serviço válido.");
                }

                var servico = _servicoHandler.ObterPorId(item.IdServico.Value);
                if (servico == null || servico.IdSalao != idSalao)
                {
                    throw new InvalidOperationException("Serviço do pedido não encontrado para este salão.");
                }

                var valorUnitario = item.ValorUnitario.GetValueOrDefault(servico.Preco);
                if (valorUnitario < 0)
                {
                    throw new InvalidOperationException($"O serviço '{servico.Nome}' possui valor inválido.");
                }

                itens.Add(new VendaProdutoItem
                {
                    TipoItem = VendaProdutoTipoItem.Servico,
                    IdServico = servico.IdServico,
                    Descricao = servico.Nome,
                    Quantidade = item.Quantidade,
                    ValorUnitario = valorUnitario,
                    ValorTotal = decimal.Round(item.Quantidade * valorUnitario, 2),
                    Unidade = "UN",
                    ControlaEstoque = false,
                    CodigoTributacaoMunicipio = PrimeiroCodigoServico(servico),
                    AliquotaIss = servico.AliquotaISS > 0 ? servico.AliquotaISS : 5m,
                    Ncm = servico.CodNBS,
                    Cfop = "5933"
                });
            }
        }

        return itens;
    }

    private static PedidoItem MapPedidoItem(VendaProdutoItem item)
    {
        return new PedidoItem
        {
            TipoItem = item.TipoItem,
            IdProduto = item.IdProduto,
            IdServico = item.IdServico,
            Descricao = item.Descricao,
            Quantidade = item.Quantidade,
            ValorUnitario = item.ValorUnitario,
            ValorTotal = item.ValorTotal,
            Unidade = item.Unidade,
            ControlaEstoque = item.ControlaEstoque,
            CodigoTributacaoMunicipio = item.CodigoTributacaoMunicipio,
            AliquotaIss = item.AliquotaIss,
            Ncm = item.Ncm,
            Cfop = item.Cfop
        };
    }

    private static string? PrimeiroCodigoServico(Servico servico)
    {
        var candidatos = new[]
        {
            servico.CodTributacaoNacional,
            servico.CodigoTributacaoMunicipio,
            servico.ItemListaServicoLC116
        };

        foreach (var candidato in candidatos)
        {
            if (!string.IsNullOrWhiteSpace(candidato))
            {
                var digitos = new string(candidato.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrWhiteSpace(digitos))
                {
                    return digitos;
                }
            }
        }

        return null;
    }

    private void TentarRegistrarInteracaoCrm(int idSalao, int? idPessoa, CrmInteracao interacao)
    {
        if (!idPessoa.HasValue || idPessoa.Value <= 0)
        {
            return;
        }

        try
        {
            interacao.IdPessoa = idPessoa.Value;
            _crmService.RegistrarInteracao(idSalao, interacao);
        }
        catch
        {
            // O CRM nao deve bloquear a operacao principal.
        }
    }
}
