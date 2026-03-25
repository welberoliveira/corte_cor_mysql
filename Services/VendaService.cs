using CorteCor.Handlers;
using CorteCor.Models;

namespace CorteCor.Services;

public class VendaService
{
    private readonly VendaEstoqueHandler _vendaEstoqueHandler;
    private readonly PessoaHandler _pessoaHandler;
    private readonly ProdutoHandler _produtoHandler;
    private readonly ServicoHandler _servicoHandler;
    private readonly MeioPagamentoHandler _meioPagamentoHandler;
    private readonly FinanceiroService _financeiroService;
    private readonly NotaFiscalHandler _notaFiscalHandler;
    private readonly VendaFiscalPreparationService _vendaFiscalPreparationService;
    private readonly CrmService _crmService;

    public VendaService(
        VendaEstoqueHandler vendaEstoqueHandler,
        PessoaHandler pessoaHandler,
        ProdutoHandler produtoHandler,
        ServicoHandler servicoHandler,
        MeioPagamentoHandler meioPagamentoHandler,
        FinanceiroService financeiroService,
        NotaFiscalHandler notaFiscalHandler,
        VendaFiscalPreparationService vendaFiscalPreparationService,
        CrmService crmService)
    {
        _vendaEstoqueHandler = vendaEstoqueHandler;
        _pessoaHandler = pessoaHandler;
        _produtoHandler = produtoHandler;
        _servicoHandler = servicoHandler;
        _meioPagamentoHandler = meioPagamentoHandler;
        _financeiroService = financeiroService;
        _notaFiscalHandler = notaFiscalHandler;
        _vendaFiscalPreparationService = vendaFiscalPreparationService;
        _crmService = crmService;
    }

    public async Task<VendaCheckoutContexto> ObterContextoAsync(int idSalao, VendaProdutoFiltro filtro)
    {
        var clientes = _pessoaHandler.ListarPorSalao(idSalao)?.Where(p => p.IsCliente && !p.Excluido).OrderBy(p => p.Nome).ToList() ?? new List<Pessoa>();
        var produtos = _produtoHandler.ListarPorSalao(idSalao)?.Where(p => !p.Arquivado).OrderBy(p => p.Nome).ToList() ?? new List<Produto>();
        var servicos = _servicoHandler.ListarPorSalao(idSalao)?.Where(s => !s.Arquivado).OrderBy(s => s.Nome).ToList() ?? new List<Servico>();
        var meios = _meioPagamentoHandler.ListarPorSalao(idSalao, true)?.OrderBy(m => m.Nome).ToList() ?? new List<MeioPagamento>();
        var vendas = await _vendaEstoqueHandler.ListarVendasAsync(idSalao, filtro);

        foreach (var venda in vendas.Items)
        {
            venda.ClasseStatusFiscal = string.IsNullOrWhiteSpace(venda.StatusFiscal)
                ? "bg-secondary"
                : NotaFiscalAvulsaService.ObterClasseStatus(venda.StatusFiscal);
        }

        return new VendaCheckoutContexto
        {
            Clientes = clientes,
            Produtos = produtos,
            Servicos = servicos,
            MeiosPagamento = meios,
            VendasRecentes = vendas
        };
    }

    public async Task<VendaOperacaoResult> FinalizarVendaAsync(
        int idSalao,
        VendaCheckoutInput input,
        string? usuario,
        string origem = "Manual",
        string? observacoesComplementares = null)
    {
        if (input.Itens == null || input.Itens.Count == 0)
        {
            throw new InvalidOperationException("Adicione ao menos um produto ou serviço à venda.");
        }

        if (input.EmitirNotaFiscalServico && !input.IdPessoa.HasValue)
        {
            throw new InvalidOperationException("Selecione um cliente para emitir nota fiscal de serviço.");
        }

        var itensNormalizados = new List<VendaProdutoItem>();
        var movimentos = new List<MovimentoEstoque>();
        decimal subtotalProdutos = 0m;
        decimal subtotalServicos = 0m;

        foreach (var item in input.Itens)
        {
            if (item.Quantidade <= 0)
            {
                throw new InvalidOperationException("Todos os itens da venda precisam ter quantidade maior que zero.");
            }

            if (string.Equals(item.TipoItem, VendaProdutoTipoItem.Produto, StringComparison.OrdinalIgnoreCase))
            {
                if (!item.IdProduto.HasValue || item.IdProduto <= 0)
                {
                    throw new InvalidOperationException("Selecione um produto válido para cada item de produto.");
                }

                var produto = _produtoHandler.ObterPorIdESalao(item.IdProduto.Value, idSalao)
                    ?? throw new InvalidOperationException("Produto da venda não encontrado para este salão.");
                var valorUnitario = item.ValorUnitario.GetValueOrDefault(produto.PrecoVenda);
                if (valorUnitario < 0)
                {
                    throw new InvalidOperationException($"O produto '{produto.Nome}' possui valor inválido.");
                }

                var valorTotal = decimal.Round(item.Quantidade * valorUnitario, 2);
                subtotalProdutos += valorTotal;

                itensNormalizados.Add(new VendaProdutoItem
                {
                    TipoItem = VendaProdutoTipoItem.Produto,
                    IdProduto = produto.IdProduto,
                    Descricao = produto.Nome,
                    Quantidade = item.Quantidade,
                    ValorUnitario = valorUnitario,
                    ValorTotal = valorTotal,
                    Unidade = string.IsNullOrWhiteSpace(produto.UnidadeComercial) ? "UN" : produto.UnidadeComercial!,
                    ControlaEstoque = produto.ControlarEstoque,
                    Ncm = produto.NCM,
                    Cfop = "5102"
                });

                if (produto.ControlarEstoque)
                {
                    var estoqueAtual = produto.EstoqueAtual ?? 0m;
                    if (estoqueAtual < item.Quantidade)
                    {
                        throw new InvalidOperationException($"Estoque insuficiente para o produto '{produto.Nome}'. Disponível: {estoqueAtual:N2}.");
                    }

                    movimentos.Add(new MovimentoEstoque
                    {
                        IdSalao = idSalao,
                        IdProduto = produto.IdProduto,
                        TipoMovimento = MovimentoEstoqueTipo.Saida,
                        Origem = MovimentoEstoqueOrigem.Venda,
                        Quantidade = item.Quantidade,
                        SaldoAnterior = estoqueAtual,
                        SaldoPosterior = estoqueAtual - item.Quantidade,
                        Observacao = $"Baixa automática pela venda de produtos.",
                        UsuarioOperador = usuario,
                        DataMovimento = DateTime.Now
                    });
                }
            }
            else
            {
                if (!item.IdServico.HasValue || item.IdServico <= 0)
                {
                    throw new InvalidOperationException("Selecione um serviço válido para cada item de serviço.");
                }

                var servico = _servicoHandler.ObterPorId(item.IdServico.Value);
                if (servico == null || servico.IdSalao != idSalao)
                {
                    throw new InvalidOperationException("Serviço da venda não encontrado para este salão.");
                }

                var valorUnitario = item.ValorUnitario.GetValueOrDefault(servico.Preco);
                if (valorUnitario < 0)
                {
                    throw new InvalidOperationException($"O serviço '{servico.Nome}' possui valor inválido.");
                }

                var valorTotal = decimal.Round(item.Quantidade * valorUnitario, 2);
                subtotalServicos += valorTotal;

                itensNormalizados.Add(new VendaProdutoItem
                {
                    TipoItem = VendaProdutoTipoItem.Servico,
                    IdServico = servico.IdServico,
                    Descricao = servico.Nome,
                    Quantidade = item.Quantidade,
                    ValorUnitario = valorUnitario,
                    ValorTotal = valorTotal,
                    Unidade = "UN",
                    ControlaEstoque = false,
                    CodigoTributacaoMunicipio = PrimeiroCodigoServico(servico),
                    AliquotaIss = servico.AliquotaISS > 0 ? servico.AliquotaISS : 5m,
                    Ncm = servico.CodNBS,
                    Cfop = "5933"
                });
            }
        }

        var desconto = input.Desconto < 0 ? 0m : input.Desconto;
        var acrescimo = input.Acrescimo < 0 ? 0m : input.Acrescimo;
        var valorTotalVenda = decimal.Round(subtotalProdutos + subtotalServicos - desconto + acrescimo, 2);
        if (valorTotalVenda <= 0)
        {
            throw new InvalidOperationException("O valor total da venda precisa ser maior que zero.");
        }

        var meioPagamento = input.IdMeioPagamento.HasValue && input.IdMeioPagamento > 0
            ? _meioPagamentoHandler.ObterPorId(input.IdMeioPagamento.Value)
            : null;
        if (meioPagamento != null && meioPagamento.IdSalao != idSalao)
        {
            throw new InvalidOperationException("Meio de pagamento inválido para este salão.");
        }

        var observacoes = input.Observacoes?.Trim();
        if (!string.IsNullOrWhiteSpace(observacoesComplementares))
        {
            observacoes = string.IsNullOrWhiteSpace(observacoes)
                ? observacoesComplementares.Trim()
                : $"{observacoes}{Environment.NewLine}{observacoesComplementares.Trim()}";
        }

        var venda = new VendaProduto
        {
            IdSalao = idSalao,
            IdPessoa = input.IdPessoa,
            IdMeioPagamento = meioPagamento?.IdMeioPagamento,
            TipoPagamento = !string.IsNullOrWhiteSpace(input.TipoPagamento)
                ? input.TipoPagamento.Trim()
                : meioPagamento?.Nome,
            RecebidoNaHora = input.RecebidoNaHora,
            SolicitarEmissaoFiscalServico = input.EmitirNotaFiscalServico,
            Status = VendaProdutoStatus.Finalizada,
            SubtotalProdutos = decimal.Round(subtotalProdutos, 2),
            SubtotalServicos = decimal.Round(subtotalServicos, 2),
            Desconto = desconto,
            Acrescimo = acrescimo,
            ValorTotal = valorTotalVenda,
            Observacoes = observacoes,
            Origem = string.IsNullOrWhiteSpace(origem) ? "Manual" : origem.Trim(),
            UsuarioOperador = usuario,
            DataVenda = DateTime.Now
        };

        var idVenda = await _vendaEstoqueHandler.CriarVendaAsync(venda, itensNormalizados, movimentos);

        Guid? idTitulo = null;
        if (input.RecebidoNaHora || valorTotalVenda > 0)
        {
            idTitulo = await _financeiroService.SalvarTituloAsync(idSalao, new FinanceiroTitulo
            {
                Tipo = FinanceiroTipoTitulo.Receber,
                Origem = FinanceiroOrigemTitulo.Venda,
                IdPessoa = input.IdPessoa,
                IdVendaProduto = idVenda,
                Descricao = $"Venda {idVenda}",
                Documento = $"VD-{idVenda}",
                Status = input.RecebidoNaHora ? FinanceiroStatusTitulo.Liquidado : FinanceiroStatusTitulo.Aberto,
                ValorOriginal = valorTotalVenda,
                ValorLiquidado = input.RecebidoNaHora ? valorTotalVenda : 0m,
                ValorAberto = input.RecebidoNaHora ? 0m : valorTotalVenda,
                DataCompetencia = DateTime.Today,
                DataVencimento = DateTime.Today,
                DataLiquidacao = input.RecebidoNaHora ? DateTime.Now : null,
                Conciliado = input.RecebidoNaHora,
                Observacoes = $"Gerado automaticamente pela venda {idVenda}."
            });
        }

        TentarRegistrarInteracaoCrm(idSalao, input.IdPessoa, new CrmInteracao
        {
            Canal = CrmCanal.Sistema,
            Tipo = "Venda",
            Assunto = "Venda concluída",
            Descricao = $"Venda #{idVenda} concluída com total de {valorTotalVenda:N2}. Produtos: {subtotalProdutos:N2}. Serviços: {subtotalServicos:N2}. Pagamento: {venda.TipoPagamento ?? "Livre"}.",
            Referencia = $"Venda:{idVenda}",
            OrigemSistema = true,
            DataInteracao = DateTime.Now
        });

        var result = new VendaOperacaoResult
        {
            Success = true,
            IdVendaProduto = idVenda,
            IdTituloFinanceiro = idTitulo,
            Mensagem = "Venda concluída com sucesso.",
            MensagemTipo = "success"
        };

        if (input.EmitirNotaFiscalServico && subtotalServicos > 0)
        {
            var fiscal = await _vendaFiscalPreparationService.EmitirNotaServicoAsync(idSalao, idVenda, usuario);
            if (fiscal.IdNotaFiscal.HasValue)
            {
                result.NotasFiscaisGeradas.Add(fiscal.IdNotaFiscal.Value);
            }

            TentarRegistrarInteracaoCrm(idSalao, input.IdPessoa, new CrmInteracao
            {
                Canal = CrmCanal.Sistema,
                Tipo = "Fiscal",
                Assunto = "NFS-e emitida pela venda",
                Descricao = $"A venda #{idVenda} gerou a nota fiscal {fiscal.NotaFiscal?.Numero}/{fiscal.NotaFiscal?.Serie} com status {fiscal.NotaFiscal?.Status ?? "Processada"}.",
                Referencia = $"Venda:{idVenda}",
                OrigemSistema = true,
                DataInteracao = DateTime.Now
            });

            result.Logs.Add(fiscal.Mensagem);
            result.Mensagem = fiscal.NotaFiscal?.Status == NotaFiscalStatus.Autorizada
                ? $"Venda concluída e NFS-e emitida em homologação com sucesso."
                : $"Venda concluída. Resultado fiscal: {fiscal.Mensagem}";
            result.MensagemTipo = fiscal.NotaFiscal?.Status == NotaFiscalStatus.Autorizada ? "success" : "warning";
        }
        else if (input.EmitirNotaFiscalServico && subtotalServicos <= 0)
        {
            result.Mensagem = "Venda concluída. Nenhum serviço foi encontrado para emissão fiscal.";
            result.MensagemTipo = "warning";
        }

        return result;
    }

    public async Task<VendaOperacaoResult> EmitirNotaServicoAsync(int idSalao, int idVendaProduto, string? usuario)
    {
        var fiscal = await _vendaFiscalPreparationService.EmitirNotaServicoAsync(idSalao, idVendaProduto, usuario);
        return new VendaOperacaoResult
        {
            Success = fiscal.NotaFiscal?.Status == NotaFiscalStatus.Autorizada,
            IdVendaProduto = idVendaProduto,
            Mensagem = fiscal.Mensagem,
            MensagemTipo = fiscal.NotaFiscal?.Status == NotaFiscalStatus.Autorizada ? "success" : "warning",
            NotasFiscaisGeradas = fiscal.IdNotaFiscal.HasValue ? new List<Guid> { fiscal.IdNotaFiscal.Value } : new List<Guid>()
        };
    }

    public async Task<VendaDetalheContexto> ObterDetalheAsync(int idSalao, int idVendaProduto)
    {
        var venda = await _vendaEstoqueHandler.ObterVendaAsync(idSalao, idVendaProduto)
            ?? throw new InvalidOperationException("Venda não encontrada.");

        var itens = await _vendaEstoqueHandler.ListarItensVendaAsync(idSalao, idVendaProduto);
        var titulos = await _financeiroService.ListarTitulosPorVendaAsync(idSalao, idVendaProduto);
        var notas = await _notaFiscalHandler.ListarPorVendaAsync(idSalao, idVendaProduto);
        var historico = await _vendaEstoqueHandler.ListarPosVendaAsync(idSalao, idVendaProduto);

        return new VendaDetalheContexto
        {
            Venda = venda,
            Itens = itens,
            Titulos = titulos,
            Notas = notas,
            HistoricoPosVenda = historico,
            Produtos = _produtoHandler.ListarPorSalao(idSalao)?.Where(p => !p.Arquivado).OrderBy(p => p.Nome).ToList() ?? new List<Produto>(),
            Servicos = _servicoHandler.ListarPorSalao(idSalao)?.Where(s => !s.Arquivado).OrderBy(s => s.Nome).ToList() ?? new List<Servico>()
        };
    }

    public async Task<VendaPosVendaOperacaoResult> ProcessarPosVendaAsync(int idSalao, int idVendaProduto, VendaPosVendaInput input, string? usuario)
    {
        var venda = await _vendaEstoqueHandler.ObterVendaAsync(idSalao, idVendaProduto)
            ?? throw new InvalidOperationException("Venda não encontrada.");
        if (string.Equals(venda.Status, VendaProdutoStatus.Cancelada, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A venda já está cancelada.");
        }

        var tipoOperacao = NormalizarTipoPosVenda(input.TipoOperacao);
        var itensVenda = await _vendaEstoqueHandler.ListarItensVendaAsync(idSalao, idVendaProduto);
        var notaAtiva = await _notaFiscalHandler.ObterNotaAtivaPorVendaAsync(idSalao, idVendaProduto);

        if (string.Equals(tipoOperacao, VendaPosVendaTipo.CancelamentoTotal, StringComparison.OrdinalIgnoreCase) && notaAtiva != null)
        {
            throw new InvalidOperationException("Cancele a nota fiscal vinculada antes de fazer o cancelamento total da venda.");
        }

        var itensOriginaisSelecionados = input.ItensOriginais?
            .Where(i => i.IdItemVenda > 0 && i.Quantidade > 0)
            .ToList() ?? new List<VendaPosVendaItemInput>();

        if (string.Equals(tipoOperacao, VendaPosVendaTipo.CancelamentoTotal, StringComparison.OrdinalIgnoreCase))
        {
            itensOriginaisSelecionados = itensVenda
                .Where(i => i.QuantidadeDisponivelPosVenda > 0)
                .Select(i => new VendaPosVendaItemInput
                {
                    IdItemVenda = i.IdItemVenda,
                    Quantidade = i.QuantidadeDisponivelPosVenda
                })
                .ToList();
        }

        if (!itensOriginaisSelecionados.Any())
        {
            throw new InvalidOperationException("Selecione ao menos um item e quantidade para o pós-venda.");
        }

        var exigeReposicao = string.Equals(tipoOperacao, VendaPosVendaTipo.Troca, StringComparison.OrdinalIgnoreCase);
        var itensReposicao = input.ItensReposicao?
            .Where(i => i.Quantidade > 0)
            .ToList() ?? new List<VendaItemInput>();

        if (exigeReposicao && !itensReposicao.Any())
        {
            throw new InvalidOperationException("A troca precisa ter ao menos um item de reposição.");
        }

        if (!exigeReposicao && itensReposicao.Any())
        {
            throw new InvalidOperationException("Itens de reposição só podem ser enviados em operações de troca.");
        }

        var itensPosVenda = new List<VendaPosVendaItem>();
        var movimentos = new List<MovimentoEstoque>();
        var saldoProdutos = new Dictionary<int, decimal>();
        var valorCredito = 0m;
        var valorReposicao = 0m;

        foreach (var selecionado in itensOriginaisSelecionados)
        {
            var itemVenda = itensVenda.FirstOrDefault(i => i.IdItemVenda == selecionado.IdItemVenda)
                ?? throw new InvalidOperationException("Item da venda não encontrado para o pós-venda.");

            if (selecionado.Quantidade > itemVenda.QuantidadeDisponivelPosVenda)
            {
                throw new InvalidOperationException($"A quantidade disponível para '{itemVenda.Descricao}' é {itemVenda.QuantidadeDisponivelPosVenda:N3}.");
            }

            if (notaAtiva != null && string.Equals(itemVenda.TipoItem, VendaProdutoTipoItem.Servico, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A venda possui nota fiscal ativa e não permite pós-venda sobre itens de serviço enquanto a nota não for cancelada.");
            }

            var valorTotalItem = decimal.Round(selecionado.Quantidade * itemVenda.ValorUnitario, 2);
            valorCredito += valorTotalItem;

            itensPosVenda.Add(new VendaPosVendaItem
            {
                IdItemVenda = itemVenda.IdItemVenda,
                TipoRegistro = VendaPosVendaRegistroTipo.Origem,
                TipoItem = itemVenda.TipoItem,
                IdProduto = itemVenda.IdProduto,
                IdServico = itemVenda.IdServico,
                Descricao = itemVenda.Descricao,
                Quantidade = selecionado.Quantidade,
                ValorUnitario = itemVenda.ValorUnitario,
                ValorTotal = valorTotalItem,
                Unidade = itemVenda.Unidade,
                ControlaEstoque = itemVenda.ControlaEstoque
            });

            if (itemVenda.ControlaEstoque && itemVenda.IdProduto.HasValue)
            {
                var produto = _produtoHandler.ObterPorIdESalao(itemVenda.IdProduto.Value, idSalao)
                    ?? throw new InvalidOperationException("Produto do pós-venda não encontrado.");
                var saldoAnterior = ObterSaldoAtualProduto(saldoProdutos, produto.IdProduto, produto.EstoqueAtual ?? 0m);
                var saldoPosterior = saldoAnterior + selecionado.Quantidade;
                saldoProdutos[produto.IdProduto] = saldoPosterior;

                movimentos.Add(new MovimentoEstoque
                {
                    IdSalao = idSalao,
                    IdProduto = produto.IdProduto,
                    IdVendaProduto = idVendaProduto,
                    TipoMovimento = MovimentoEstoqueTipo.Entrada,
                    Origem = MovimentoEstoqueOrigem.PosVenda,
                    Quantidade = selecionado.Quantidade,
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = saldoPosterior,
                    Observacao = $"Pós-venda da venda {idVendaProduto}: {tipoOperacao} do item {itemVenda.Descricao}.",
                    UsuarioOperador = usuario,
                    DataMovimento = DateTime.Now
                });
            }
        }

        foreach (var reposicao in itensReposicao)
        {
            if (string.Equals(reposicao.TipoItem, VendaProdutoTipoItem.Produto, StringComparison.OrdinalIgnoreCase))
            {
                if (!reposicao.IdProduto.HasValue || reposicao.IdProduto <= 0)
                {
                    throw new InvalidOperationException("Selecione um produto válido para a reposição da troca.");
                }

                var produto = _produtoHandler.ObterPorIdESalao(reposicao.IdProduto.Value, idSalao)
                    ?? throw new InvalidOperationException("Produto de reposição não encontrado.");
                var valorUnitario = reposicao.ValorUnitario.GetValueOrDefault(produto.PrecoVenda);
                if (valorUnitario < 0)
                {
                    throw new InvalidOperationException($"O produto '{produto.Nome}' possui valor inválido.");
                }

                var valorTotal = decimal.Round(reposicao.Quantidade * valorUnitario, 2);
                valorReposicao += valorTotal;

                itensPosVenda.Add(new VendaPosVendaItem
                {
                    TipoRegistro = VendaPosVendaRegistroTipo.Reposicao,
                    TipoItem = VendaProdutoTipoItem.Produto,
                    IdProduto = produto.IdProduto,
                    Descricao = produto.Nome,
                    Quantidade = reposicao.Quantidade,
                    ValorUnitario = valorUnitario,
                    ValorTotal = valorTotal,
                    Unidade = string.IsNullOrWhiteSpace(produto.UnidadeComercial) ? "UN" : produto.UnidadeComercial!,
                    ControlaEstoque = produto.ControlarEstoque
                });

                if (produto.ControlarEstoque)
                {
                    var saldoAnterior = ObterSaldoAtualProduto(saldoProdutos, produto.IdProduto, produto.EstoqueAtual ?? 0m);
                    if (saldoAnterior < reposicao.Quantidade)
                    {
                        throw new InvalidOperationException($"Estoque insuficiente para a reposição do produto '{produto.Nome}'. Disponível: {saldoAnterior:N3}.");
                    }

                    var saldoPosterior = saldoAnterior - reposicao.Quantidade;
                    saldoProdutos[produto.IdProduto] = saldoPosterior;
                    movimentos.Add(new MovimentoEstoque
                    {
                        IdSalao = idSalao,
                        IdProduto = produto.IdProduto,
                        IdVendaProduto = idVendaProduto,
                        TipoMovimento = MovimentoEstoqueTipo.Saida,
                        Origem = MovimentoEstoqueOrigem.PosVenda,
                        Quantidade = reposicao.Quantidade,
                        SaldoAnterior = saldoAnterior,
                        SaldoPosterior = saldoPosterior,
                        Observacao = $"Pós-venda da venda {idVendaProduto}: reposição na troca do item {produto.Nome}.",
                        UsuarioOperador = usuario,
                        DataMovimento = DateTime.Now
                    });
                }
            }
            else
            {
                if (notaAtiva != null)
                {
                    throw new InvalidOperationException("Não é permitido adicionar serviços na troca enquanto a venda possuir nota fiscal ativa.");
                }

                if (!reposicao.IdServico.HasValue || reposicao.IdServico <= 0)
                {
                    throw new InvalidOperationException("Selecione um serviço válido para a reposição da troca.");
                }

                var servico = _servicoHandler.ObterPorId(reposicao.IdServico.Value);
                if (servico == null || servico.IdSalao != idSalao)
                {
                    throw new InvalidOperationException("Serviço de reposição não encontrado para este salão.");
                }

                var valorUnitario = reposicao.ValorUnitario.GetValueOrDefault(servico.Preco);
                if (valorUnitario < 0)
                {
                    throw new InvalidOperationException($"O serviço '{servico.Nome}' possui valor inválido.");
                }

                var valorTotal = decimal.Round(reposicao.Quantidade * valorUnitario, 2);
                valorReposicao += valorTotal;

                itensPosVenda.Add(new VendaPosVendaItem
                {
                    TipoRegistro = VendaPosVendaRegistroTipo.Reposicao,
                    TipoItem = VendaProdutoTipoItem.Servico,
                    IdServico = servico.IdServico,
                    Descricao = servico.Nome,
                    Quantidade = reposicao.Quantidade,
                    ValorUnitario = valorUnitario,
                    ValorTotal = valorTotal,
                    Unidade = "UN",
                    ControlaEstoque = false
                });
            }
        }

        var diferencaFinanceira = decimal.Round(valorReposicao - valorCredito, 2);
        var posVenda = new VendaPosVenda
        {
            TipoOperacao = tipoOperacao,
            Status = VendaPosVendaStatus.Processada,
            ValorCredito = decimal.Round(valorCredito, 2),
            ValorReposicao = decimal.Round(valorReposicao, 2),
            DiferencaFinanceira = diferencaFinanceira,
            Observacoes = input.Observacoes?.Trim(),
            UsuarioOperador = usuario,
            DataOperacao = DateTime.Now
        };

        var descricaoOperacao = $"{ObterTituloOperacao(tipoOperacao)} da venda #{idVendaProduto}";
        var observacaoVenda = string.IsNullOrWhiteSpace(input.Observacoes)
            ? $"Pós-venda processado: {ObterTituloOperacao(tipoOperacao)}."
            : $"Pós-venda processado: {ObterTituloOperacao(tipoOperacao)}. {input.Observacoes.Trim()}";

        var novoStatus = string.Equals(tipoOperacao, VendaPosVendaTipo.CancelamentoTotal, StringComparison.OrdinalIgnoreCase)
            ? VendaProdutoStatus.Cancelada
            : VendaProdutoStatus.Ajustada;

        var idPosVenda = await _vendaEstoqueHandler.ProcessarPosVendaAsync(
            idSalao,
            idVendaProduto,
            posVenda,
            itensPosVenda,
            movimentos,
            novoStatus,
            observacaoVenda);

        var resumoFinanceiro = await _financeiroService.AplicarAjustePosVendaAsync(
            idSalao,
            idVendaProduto,
            venda.IdPessoa,
            diferencaFinanceira,
            descricaoOperacao,
            posVenda.Observacoes);

        TentarRegistrarInteracaoCrm(idSalao, venda.IdPessoa, new CrmInteracao
        {
            Canal = CrmCanal.Sistema,
            Tipo = "PosVenda",
            Assunto = ObterTituloOperacao(tipoOperacao),
            Descricao = $"A venda #{idVendaProduto} recebeu uma operação de pós-venda do tipo {ObterTituloOperacao(tipoOperacao)}. Crédito: {valorCredito:N2}. Reposição: {valorReposicao:N2}. {resumoFinanceiro}",
            Referencia = $"Venda:{idVendaProduto}",
            OrigemSistema = true,
            DataInteracao = DateTime.Now
        });

        return new VendaPosVendaOperacaoResult
        {
            Success = true,
            IdVendaProduto = idVendaProduto,
            IdPosVenda = idPosVenda,
            Mensagem = $"{ObterTituloOperacao(tipoOperacao)} processado com sucesso.",
            MensagemTipo = novoStatus == VendaProdutoStatus.Cancelada ? "warning" : "success",
            ValorCredito = valorCredito,
            ValorReposicao = valorReposicao,
            DiferencaFinanceira = diferencaFinanceira,
            ResumoFinanceiro = resumoFinanceiro
        };
    }

    public async Task CancelarVendaAsync(int idSalao, int idVendaProduto, string? usuario, string? observacao = null)
    {
        await ProcessarPosVendaAsync(idSalao, idVendaProduto, new VendaPosVendaInput
        {
            TipoOperacao = VendaPosVendaTipo.CancelamentoTotal,
            Observacoes = observacao
        }, usuario);
    }

    public Task<VendaProduto?> ObterVendaAsync(int idSalao, int idVendaProduto) =>
        _vendaEstoqueHandler.ObterVendaAsync(idSalao, idVendaProduto);

    public Task<List<VendaProdutoItem>> ListarItensVendaAsync(int idSalao, int idVendaProduto) =>
        _vendaEstoqueHandler.ListarItensVendaAsync(idSalao, idVendaProduto);

    public Task<PagedResult<ProdutoEstoquePosicao>> ListarPosicaoEstoqueAsync(int idSalao, string? pesquisa, int pageIndex, int pageSize) =>
        _vendaEstoqueHandler.ListarPosicaoEstoqueAsync(idSalao, pesquisa, false, pageIndex, pageSize);

    public Task<PagedResult<ProdutoEstoquePosicao>> ListarPosicaoEstoqueAsync(int idSalao, string? pesquisa, bool somenteBaixo, int pageIndex, int pageSize) =>
        _vendaEstoqueHandler.ListarPosicaoEstoqueAsync(idSalao, pesquisa, somenteBaixo, pageIndex, pageSize);

    public Task<EstoqueResumo> ObterResumoEstoqueAsync(int idSalao) =>
        _vendaEstoqueHandler.ObterResumoEstoqueAsync(idSalao);

    public Task<PagedResult<MovimentoEstoque>> ListarMovimentosAsync(int idSalao, EstoqueMovimentoFiltro filtro) =>
        _vendaEstoqueHandler.ListarMovimentosAsync(idSalao, filtro);

    public async Task RegistrarAjusteEstoqueAsync(int idSalao, int idProduto, decimal quantidade, string tipoMovimento, string? observacao, string? usuario)
    {
        if (idProduto <= 0)
        {
            throw new InvalidOperationException("Selecione um produto válido para o ajuste de estoque.");
        }

        if (quantidade <= 0)
        {
            throw new InvalidOperationException("Informe uma quantidade maior que zero para o ajuste.");
        }

        var produto = _produtoHandler.ObterPorIdESalao(idProduto, idSalao)
            ?? throw new InvalidOperationException("Produto não encontrado para este salão.");

        if (!produto.ControlarEstoque)
        {
            throw new InvalidOperationException("O produto selecionado não está configurado para controle de estoque.");
        }

        var tipo = string.Equals(tipoMovimento, MovimentoEstoqueTipo.Saida, StringComparison.OrdinalIgnoreCase)
            ? MovimentoEstoqueTipo.Saida
            : MovimentoEstoqueTipo.Entrada;

        if (tipo == MovimentoEstoqueTipo.Saida && (produto.EstoqueAtual ?? 0m) < quantidade)
        {
            throw new InvalidOperationException($"Estoque insuficiente para saída manual do produto '{produto.Nome}'.");
        }

        await _vendaEstoqueHandler.RegistrarAjusteEstoqueAsync(new MovimentoEstoque
        {
            IdSalao = idSalao,
            IdProduto = idProduto,
            TipoMovimento = tipo,
            Origem = MovimentoEstoqueOrigem.AjusteManual,
            Quantidade = quantidade,
            Observacao = string.IsNullOrWhiteSpace(observacao)
                ? $"Ajuste manual de estoque do produto {produto.Nome}."
                : observacao.Trim(),
            UsuarioOperador = usuario,
            DataMovimento = DateTime.Now
        });
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

    private static string NormalizarTipoPosVenda(string? tipoOperacao)
    {
        if (string.Equals(tipoOperacao, VendaPosVendaTipo.Troca, StringComparison.OrdinalIgnoreCase))
        {
            return VendaPosVendaTipo.Troca;
        }

        if (string.Equals(tipoOperacao, VendaPosVendaTipo.CancelamentoParcial, StringComparison.OrdinalIgnoreCase))
        {
            return VendaPosVendaTipo.CancelamentoParcial;
        }

        if (string.Equals(tipoOperacao, VendaPosVendaTipo.CancelamentoTotal, StringComparison.OrdinalIgnoreCase))
        {
            return VendaPosVendaTipo.CancelamentoTotal;
        }

        return VendaPosVendaTipo.Devolucao;
    }

    private static string ObterTituloOperacao(string tipoOperacao) =>
        tipoOperacao switch
        {
            var tipo when string.Equals(tipo, VendaPosVendaTipo.Troca, StringComparison.OrdinalIgnoreCase) => "Troca",
            var tipo when string.Equals(tipo, VendaPosVendaTipo.CancelamentoParcial, StringComparison.OrdinalIgnoreCase) => "Cancelamento parcial",
            var tipo when string.Equals(tipo, VendaPosVendaTipo.CancelamentoTotal, StringComparison.OrdinalIgnoreCase) => "Cancelamento total",
            _ => "Devolução"
        };

    private static decimal ObterSaldoAtualProduto(IDictionary<int, decimal> saldos, int idProduto, decimal saldoBase)
    {
        if (!saldos.TryGetValue(idProduto, out var saldoAtual))
        {
            saldoAtual = saldoBase;
            saldos[idProduto] = saldoAtual;
        }

        return saldoAtual;
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
            // O registro no CRM nao deve interromper a conclusao operacional da venda.
        }
    }
}
