using System.Data;
using System.Globalization;
using System.Text;
using Dapper;
using CorteCor.Handlers;
using CorteCor.Models;

namespace CorteCor.Services;

public sealed class RelatorioCentralService
{
    private readonly IDatabaseHandler _databaseHandler;

    public RelatorioCentralService(IDatabaseHandler databaseHandler)
    {
        _databaseHandler = databaseHandler;
    }

    public IReadOnlyList<RelatorioCatalogItem> ListarCatalogo()
    {
        return new List<RelatorioCatalogItem>
        {
            Novo(RelatorioTipos.Clientes, "Clientes", "Base de clientes e contatos.", "Cadastros",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Ativo),
            Novo(RelatorioTipos.Funcionarios, "Funcionários", "Equipe e disponibilidade cadastrada.", "Cadastros",
                RelatorioFiltros.Pesquisa),
            Novo(RelatorioTipos.Servicos, "Serviços", "Serviços cadastrados com dados comerciais e fiscais.", "Cadastros",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Categoria, RelatorioFiltros.Ativo),
            Novo(RelatorioTipos.Produtos, "Produtos", "Produtos com categoria, estoque e status.", "Cadastros",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Categoria, RelatorioFiltros.Ativo),
            Novo(RelatorioTipos.CategoriasProdutos, "Categorias de Produtos", "Categorias ativas e inativas.", "Cadastros",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Ativo),

            Novo(RelatorioTipos.Pedidos, "Pedidos e Orçamentos", "Pedidos, orçamentos e validade.", "Comercial",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Cliente, RelatorioFiltros.Status, RelatorioFiltros.Data, RelatorioFiltros.SomenteVigentes),
            Novo(RelatorioTipos.Vendas, "Vendas de Produtos e Serviços", "Vendas, totais e situação fiscal.", "Comercial",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Cliente, RelatorioFiltros.Status, RelatorioFiltros.Data),
            Novo(RelatorioTipos.Estoque, "Controle de Estoque", "Posição atual e status do estoque.", "Comercial",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Categoria, RelatorioFiltros.Produto, RelatorioFiltros.Status),
            Novo(RelatorioTipos.Agendamentos, "Lista de Agendamentos", "Agendamentos por período, cliente, serviço e profissional.", "Operação",
                RelatorioFiltros.Data, RelatorioFiltros.Status, RelatorioFiltros.Cliente, RelatorioFiltros.Servico, RelatorioFiltros.Funcionario),

            Novo(RelatorioTipos.ModelosEmail, "Modelos de E-mail", "Modelos ativos e inativos por tipo de evento.", "Comunicação",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Ativo, RelatorioFiltros.Tipo),
            Novo(RelatorioTipos.ModelosSms, "Modelos de SMS", "Modelos de SMS por evento.", "Comunicação",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Ativo, RelatorioFiltros.Tipo),
            Novo(RelatorioTipos.RegrasLembrete, "Regras de Lembrete", "Configurações de lembretes automáticos.", "Comunicação",
                RelatorioFiltros.Ativo, RelatorioFiltros.TipoLembrete, RelatorioFiltros.Data),
            Novo(RelatorioTipos.LogEnvios, "Logs de Envios", "Histórico de envios de e-mail, SMS e WhatsApp.", "Comunicação",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Status, RelatorioFiltros.TipoLembrete, RelatorioFiltros.Data),
            Novo(RelatorioTipos.LogsSistema, "Logs do Sistema", "Logs globais e integrações do sistema.", "Comunicação",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Usuario, RelatorioFiltros.CodigoErro, RelatorioFiltros.Data),

            Novo(RelatorioTipos.CrmTarefas, "CRM - Tarefas", "Acompanhamentos pendentes e concluídos.", "CRM",
                RelatorioFiltros.Cliente, RelatorioFiltros.Status, RelatorioFiltros.Pesquisa, RelatorioFiltros.Data),
            Novo(RelatorioTipos.CrmOportunidades, "CRM - Oportunidades", "Funil de oportunidades por cliente e período.", "CRM",
                RelatorioFiltros.Cliente, RelatorioFiltros.Status, RelatorioFiltros.Data),
            Novo(RelatorioTipos.CrmCampanhas, "CRM - Campanhas", "Campanhas por canal, segmento e status.", "CRM",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Canal, RelatorioFiltros.Segmento, RelatorioFiltros.Status, RelatorioFiltros.Data),

            Novo(RelatorioTipos.FinanceiroPagarReceber, "Financeiro - Contas a Pagar e Receber", "Títulos financeiros detalhados.", "Financeiro",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Tipo, RelatorioFiltros.Status, RelatorioFiltros.Plano, RelatorioFiltros.Conta, RelatorioFiltros.Data),
            Novo(RelatorioTipos.FinanceiroRelatorios, "Financeiro - Consolidado", "Visão consolidada para análise financeira.", "Financeiro",
                RelatorioFiltros.Tipo, RelatorioFiltros.Status, RelatorioFiltros.Plano, RelatorioFiltros.Conta, RelatorioFiltros.Data),
            Novo(RelatorioTipos.FinanceiroPlanoContas, "Financeiro - Plano de Contas", "Plano de contas cadastrado.", "Financeiro",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Tipo, RelatorioFiltros.Ativo),
            Novo(RelatorioTipos.FinanceiroContasCaixa, "Financeiro - Contas Caixa", "Contas caixa e bancos.", "Financeiro",
                RelatorioFiltros.Pesquisa, RelatorioFiltros.Tipo, RelatorioFiltros.Ativo),
            Novo(RelatorioTipos.PagamentosPendencias, "Pagamentos e Pendências", "Pagamentos por cliente, data e status.", "Financeiro",
                RelatorioFiltros.Cliente, RelatorioFiltros.Status, RelatorioFiltros.Data),

            Novo(RelatorioTipos.ConfiguracoesSistema, "Configurações do Sistema", "Resumo de dados da empresa e configuração fiscal.", "Fiscal e Configuração",
                RelatorioFiltros.Ambiente, RelatorioFiltros.EmissaoAutomatica),
            Novo(RelatorioTipos.NotasFiscais, "Nota Fiscal Avulsa Emitida", "Notas fiscais emitidas, rejeitadas ou canceladas.", "Fiscal e Configuração",
                RelatorioFiltros.Status, RelatorioFiltros.Data, RelatorioFiltros.Ambiente),
            Novo(RelatorioTipos.AuditoriaFiscal, "Auditoria Fiscal", "Eventos e auditoria da camada fiscal.", "Fiscal e Configuração",
                RelatorioFiltros.Data, RelatorioFiltros.CodigoErro, RelatorioFiltros.Usuario),
            Novo(RelatorioTipos.DiagnosticoCertificado, "Diagnóstico", "Situação atual do certificado e parâmetros fiscais.", "Fiscal e Configuração",
                RelatorioFiltros.Ambiente, RelatorioFiltros.EmissaoAutomatica)
        };
    }

    public RelatorioCatalogItem ObterDefinicao(string? tipo)
    {
        var item = ListarCatalogo().FirstOrDefault(x => string.Equals(x.Tipo, tipo, StringComparison.OrdinalIgnoreCase));
        return item ?? ListarCatalogo().First();
    }

    public async Task<RelatorioFiltrosContexto> ObterContextoFiltrosAsync(int idSalao)
    {
        using var conn = _databaseHandler.GetConnection();

        return new RelatorioFiltrosContexto
        {
            Clientes = (await conn.QueryAsync<RelatorioOpcao>(
                @"SELECT CAST(IdPessoa AS varchar(20)) AS Valor, Nome AS Rotulo
                  FROM CorteCor_Pessoa
                  WHERE IdSalao = @IdSalao AND ISNULL(Excluido, 0) = 0
                  ORDER BY Nome;",
                new { IdSalao = idSalao })).ToList(),
            Funcionarios = (await conn.QueryAsync<RelatorioOpcao>(
                @"SELECT CAST(IdFuncionario AS varchar(20)) AS Valor, Nome AS Rotulo
                  FROM CorteCor_Funcionario
                  WHERE IdSalao = @IdSalao
                  ORDER BY Nome;",
                new { IdSalao = idSalao })).ToList(),
            Categorias = (await conn.QueryAsync<RelatorioOpcao>(
                @"SELECT CAST(IdCategoria AS varchar(20)) AS Valor, Nome AS Rotulo
                  FROM CorteCor_CategoriaProduto
                  WHERE IdSalao = @IdSalao
                  ORDER BY Nome;",
                new { IdSalao = idSalao })).ToList(),
            Servicos = (await conn.QueryAsync<RelatorioOpcao>(
                @"SELECT CAST(IdServico AS varchar(20)) AS Valor, Nome AS Rotulo
                  FROM CorteCor_Servico
                  WHERE IdSalao = @IdSalao
                  ORDER BY Nome;",
                new { IdSalao = idSalao })).ToList(),
            Produtos = (await conn.QueryAsync<RelatorioOpcao>(
                @"SELECT CAST(IdProduto AS varchar(20)) AS Valor, Nome AS Rotulo
                  FROM CorteCor_Produto
                  WHERE IdSalao = @IdSalao
                  ORDER BY Nome;",
                new { IdSalao = idSalao })).ToList(),
            Planos = (await conn.QueryAsync<RelatorioOpcao>(
                @"SELECT CAST(IdPlano AS varchar(20)) AS Valor, Descricao AS Rotulo
                  FROM CorteCor_PlanoContas
                  WHERE IdSalao = @IdSalao
                  ORDER BY Descricao;",
                new { IdSalao = idSalao })).ToList(),
            Contas = (await conn.QueryAsync<RelatorioOpcao>(
                @"SELECT CAST(IdConta AS varchar(20)) AS Valor, Nome AS Rotulo
                  FROM CorteCor_ContaCaixa
                  WHERE IdSalao = @IdSalao
                  ORDER BY Nome;",
                new { IdSalao = idSalao })).ToList()
        };
    }

    public async Task<RelatorioResultado> GerarAsync(int idSalao, RelatorioFiltroInput filtro)
    {
        var definicao = ObterDefinicao(filtro.Tipo);
        using var conn = _databaseHandler.GetConnection();
        var parameters = CriarParametrosBase(idSalao, filtro);
        var comando = MontarComando(definicao.Tipo);
        var linhasBrutas = await conn.QueryAsync(comando.Sql, parameters);
        var linhas = linhasBrutas.Select(MapearLinha).ToList();

        return new RelatorioResultado
        {
            Definicao = definicao,
            Colunas = comando.Colunas,
            Linhas = linhas,
            TotalLinhas = linhas.Count,
            MensagemVazia = $"Nenhum dado encontrado em {definicao.Titulo.ToLowerInvariant()} para os filtros informados."
        };
    }

    public byte[] GerarCsv(RelatorioResultado resultado)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", resultado.Colunas.Select(c => EscaparCsv(c.Titulo))));
        foreach (var linha in resultado.Linhas)
        {
            sb.AppendLine(string.Join(";", resultado.Colunas.Select(c =>
            {
                linha.TryGetValue(c.Chave, out var valor);
                return EscaparCsv(valor);
            })));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public byte[] GerarPdf(RelatorioResultado resultado, RelatorioFiltroInput filtro)
    {
        var canvas = new PdfCanvas();
        const float margin = 30f;
        var usableWidth = PdfCanvas.A4Width - (margin * 2);
        var y = PdfCanvas.A4Height - 36f;

        canvas.SetFillColor(10, 84, 170);
        canvas.FillRectangle(margin, y - 16f, usableWidth, 22f);
        canvas.SetFillColor(255, 255, 255);
        canvas.DrawText(margin + 10f, y - 2f, resultado.Definicao.Titulo, 14f, true);

        y -= 34f;
        canvas.SetFillColor(51, 65, 85);
        canvas.DrawText(margin, y, $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}", 8f);
        y -= 12f;

        var resumoFiltro = MontarResumoFiltros(filtro);
        if (!string.IsNullOrWhiteSpace(resumoFiltro))
        {
            y = canvas.DrawParagraph(margin, y, usableWidth, resumoFiltro, 8f, 10f) - 8f;
        }

        canvas.SetStrokeColor(190, 197, 204);
        canvas.DrawLine(margin, y, PdfCanvas.A4Width - margin, y);
        y -= 18f;

        var colunasPdf = resultado.Colunas.Take(6).ToList();
        var colWidth = usableWidth / Math.Max(1, colunasPdf.Count);
        var headerTop = y;

        canvas.SetFillColor(226, 232, 240);
        canvas.FillRectangle(margin, headerTop - 14f, usableWidth, 18f);
        canvas.SetFillColor(31, 41, 55);
        for (var i = 0; i < colunasPdf.Count; i++)
        {
            canvas.DrawText(margin + (i * colWidth) + 4f, headerTop - 2f, colunasPdf[i].Titulo, 7f, true);
        }

        y = headerTop - 22f;
        const int maxRows = 26;
        foreach (var linha in resultado.Linhas.Take(maxRows))
        {
            var lineTop = y;
            const float rowHeight = 28f;
            canvas.SetStrokeColor(220, 226, 232);
            canvas.DrawRectangle(margin, lineTop - rowHeight + 4f, usableWidth, rowHeight);

            for (var i = 0; i < colunasPdf.Count; i++)
            {
                linha.TryGetValue(colunasPdf[i].Chave, out var valor);
                var colX = margin + (i * colWidth) + 4f;
                var colY = lineTop - 2f;
                canvas.DrawParagraph(colX, colY, colWidth - 8f, valor, 7f, 8.5f, false, 3);

                if (i > 0)
                {
                    var lineX = margin + (i * colWidth);
                    canvas.DrawLine(lineX, lineTop - rowHeight + 4f, lineX, lineTop + 4f);
                }
            }

            y -= rowHeight;
            if (y < 60f)
            {
                break;
            }
        }

        canvas.SetFillColor(51, 65, 85);
        var textoFooter = resultado.Linhas.Count > maxRows
            ? $"PDF resumido: exibindo {maxRows} de {resultado.TotalLinhas} registros. Use CSV para o conjunto completo."
            : $"Total de registros: {resultado.TotalLinhas}";
        canvas.DrawText(margin, 38f, textoFooter, 8f, true);

        return canvas.Build();
    }

    private static DynamicParameters CriarParametrosBase(int idSalao, RelatorioFiltroInput filtro)
    {
        var parameters = new DynamicParameters();
        parameters.Add("IdSalao", idSalao);
        parameters.Add("Pesquisa", string.IsNullOrWhiteSpace(filtro.q) ? null : $"%{filtro.q.Trim()}%");
        parameters.Add("Status", string.IsNullOrWhiteSpace(filtro.status) ? null : filtro.status);
        parameters.Add("Tipo", string.IsNullOrWhiteSpace(filtro.tipo) ? null : filtro.tipo);
        parameters.Add("Canal", string.IsNullOrWhiteSpace(filtro.canal) ? null : filtro.canal);
        parameters.Add("Segmento", string.IsNullOrWhiteSpace(filtro.segmento) ? null : filtro.segmento);
        parameters.Add("TipoLembrete", string.IsNullOrWhiteSpace(filtro.tipoLembrete) ? null : filtro.tipoLembrete);
        parameters.Add("Usuario", string.IsNullOrWhiteSpace(filtro.usuario) ? null : $"%{filtro.usuario.Trim()}%");
        parameters.Add("CodigoErro", string.IsNullOrWhiteSpace(filtro.codigoErro) ? null : $"%{filtro.codigoErro.Trim()}%");
        parameters.Add("IdPessoa", filtro.idPessoa);
        parameters.Add("IdFuncionario", filtro.idFuncionario);
        parameters.Add("IdCategoria", filtro.idCategoria);
        parameters.Add("IdServico", filtro.idServico);
        parameters.Add("IdProduto", filtro.idProduto);
        parameters.Add("IdPlano", filtro.idPlano);
        parameters.Add("IdConta", filtro.idConta);
        parameters.Add("Ambiente", filtro.ambiente);
        parameters.Add("Ativo", filtro.ativo);
        parameters.Add("EmissaoAutomatica", filtro.emissaoAutomatica);
        parameters.Add("SomenteVigentes", filtro.somenteVigentes);
        parameters.Add("DataInicio", filtro.dataInicio?.Date);
        parameters.Add("DataFim", filtro.dataFim?.Date);
        return parameters;
    }

    private static RelatorioCatalogItem Novo(string tipo, string titulo, string descricao, string grupo, params string[] filtros)
    {
        return new RelatorioCatalogItem
        {
            Tipo = tipo,
            Titulo = titulo,
            Descricao = descricao,
            Grupo = grupo,
            Filtros = filtros.ToList()
        };
    }

    private static List<RelatorioColuna> Colunas(params string[] nomes)
    {
        return nomes.Select(nome => new RelatorioColuna
        {
            Chave = nome,
            Titulo = SepararTitulo(nome)
        }).ToList();
    }

    private static Dictionary<string, string> MapearLinha(dynamic row)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (row is IDictionary<string, object> values)
        {
            foreach (var item in values)
            {
                dict[item.Key] = FormatarValor(item.Value);
            }
        }

        return dict;
    }

    private static string FormatarValor(object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime dt => dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")),
            decimal dec => dec.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
            double dbl => dbl.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
            float flt => flt.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
            bool b => b ? "Sim" : "Não",
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string EscaparCsv(string? valor)
    {
        var sanitized = (valor ?? string.Empty).Replace("\"", "\"\"");
        return $"\"{sanitized}\"";
    }

    private static string SepararTitulo(string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append(chave[0]);
        for (var i = 1; i < chave.Length; i++)
        {
            if (char.IsUpper(chave[i]) && !char.IsUpper(chave[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(chave[i]);
        }

        return sb.ToString();
    }

    private static string MontarResumoFiltros(RelatorioFiltroInput filtro)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(filtro.q))
        {
            partes.Add($"Pesquisa: {filtro.q}");
        }
        if (!string.IsNullOrWhiteSpace(filtro.status))
        {
            partes.Add($"Status: {filtro.status}");
        }
        if (!string.IsNullOrWhiteSpace(filtro.tipo))
        {
            partes.Add($"Tipo: {filtro.tipo}");
        }
        if (!string.IsNullOrWhiteSpace(filtro.canal))
        {
            partes.Add($"Canal: {filtro.canal}");
        }
        if (filtro.dataInicio.HasValue || filtro.dataFim.HasValue)
        {
            partes.Add($"Período: {(filtro.dataInicio?.ToString("dd/MM/yyyy") ?? "-")} até {(filtro.dataFim?.ToString("dd/MM/yyyy") ?? "-")}");
        }
        if (filtro.somenteVigentes)
        {
            partes.Add("Somente vigentes");
        }

        return string.Join(" | ", partes);
    }

    private static (string Sql, List<RelatorioColuna> Colunas) MontarComando(string tipo)
    {
        return tipo switch
        {
            RelatorioTipos.Clientes => (
                @"SELECT
                    P.Nome AS Nome,
                    ISNULL(P.CpfCnpj, '') AS Documento,
                    ISNULL(P.Telefone, '') AS Telefone,
                    ISNULL(P.Email, '') AS Email,
                    CONCAT(ISNULL(P.Cidade, ''), CASE WHEN ISNULL(P.UF, '') = '' THEN '' ELSE '/' + P.UF END) AS CidadeUF,
                    CASE WHEN ISNULL(P.Excluido, 0) = 1 THEN 'Inativo' ELSE 'Ativo' END AS Situacao
                  FROM CorteCor_Pessoa P
                  WHERE P.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR P.Nome LIKE @Pesquisa OR ISNULL(P.CpfCnpj, '') LIKE @Pesquisa OR ISNULL(P.Email, '') LIKE @Pesquisa OR ISNULL(P.Telefone, '') LIKE @Pesquisa)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(P.Excluido, 0) = 0 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(P.Excluido, 0) = 1 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY P.Nome;",
                Colunas("Nome", "Documento", "Telefone", "Email", "CidadeUF", "Situacao")),

            RelatorioTipos.Funcionarios => (
                @"SELECT
                    F.Nome AS Nome,
                    CASE WHEN ISNULL(F.seg, 0) = 1 THEN 'Sim' ELSE 'Não' END AS Segunda,
                    CASE WHEN ISNULL(F.sex, 0) = 1 THEN 'Sim' ELSE 'Não' END AS Sexta,
                    CONCAT(
                        CASE WHEN F.seg_ini IS NOT NULL THEN CONVERT(varchar(5), F.seg_ini) ELSE '--:--' END,
                        ' / ',
                        CASE WHEN F.seg_fim IS NOT NULL THEN CONVERT(varchar(5), F.seg_fim) ELSE '--:--' END
                    ) AS HorarioReferencia
                  FROM CorteCor_Funcionario F
                  WHERE F.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR F.Nome LIKE @Pesquisa)
                  ORDER BY F.Nome;",
                Colunas("Nome", "Segunda", "Sexta", "HorarioReferencia")),

            RelatorioTipos.Servicos => (
                @"SELECT
                    S.Nome AS Nome,
                    ISNULL(C.Nome, '') AS Categoria,
                    FORMAT(S.Preco, 'N2', 'pt-BR') AS Preco,
                    CONVERT(varchar(5), S.Duracao) AS Duracao,
                    ISNULL(S.CodigoTributacaoMunicipio, '') AS Tributacao,
                    CASE WHEN ISNULL(S.Arquivado, 0) = 1 THEN 'Arquivado' ELSE 'Ativo' END AS Situacao
                  FROM CorteCor_Servico S
                  LEFT JOIN CorteCor_CategoriaProduto C ON C.IdCategoria = S.IdCategoria
                  WHERE S.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR S.Nome LIKE @Pesquisa OR ISNULL(S.Tags, '') LIKE @Pesquisa)
                    AND (@IdCategoria IS NULL OR S.IdCategoria = @IdCategoria)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(S.Arquivado, 0) = 0 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(S.Arquivado, 0) = 1 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY S.Nome;",
                Colunas("Nome", "Categoria", "Preco", "Duracao", "Tributacao", "Situacao")),

            RelatorioTipos.Produtos => (
                @"SELECT
                    P.Nome AS Nome,
                    ISNULL(C.Nome, '') AS Categoria,
                    FORMAT(P.PrecoVenda, 'N2', 'pt-BR') AS PrecoVenda,
                    FORMAT(P.EstoqueAtual, 'N3', 'pt-BR') AS EstoqueAtual,
                    FORMAT(P.EstoqueMinimo, 'N3', 'pt-BR') AS EstoqueMinimo,
                    CASE WHEN ISNULL(P.Arquivado, 0) = 1 THEN 'Arquivado' ELSE 'Ativo' END AS Situacao
                  FROM CorteCor_Produto P
                  LEFT JOIN CorteCor_CategoriaProduto C ON C.IdCategoria = P.IdCategoria
                  WHERE P.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR P.Nome LIKE @Pesquisa OR ISNULL(P.CodigoProprio, '') LIKE @Pesquisa OR ISNULL(P.Tags, '') LIKE @Pesquisa)
                    AND (@IdCategoria IS NULL OR P.IdCategoria = @IdCategoria)
                    AND (@IdProduto IS NULL OR P.IdProduto = @IdProduto)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(P.Arquivado, 0) = 0 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(P.Arquivado, 0) = 1 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY P.Nome;",
                Colunas("Nome", "Categoria", "PrecoVenda", "EstoqueAtual", "EstoqueMinimo", "Situacao")),

            RelatorioTipos.CategoriasProdutos => (
                @"SELECT
                    Nome,
                    CASE WHEN ISNULL(Ativo, 1) = 1 THEN 'Ativa' ELSE 'Inativa' END AS Situacao
                  FROM CorteCor_CategoriaProduto
                  WHERE IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(Ativo, 1) = 1 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(Ativo, 1) = 0 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY Nome;",
                Colunas("Nome", "Situacao")),

            RelatorioTipos.Pedidos => (
                @"SELECT
                    CAST(Pd.IdPedido AS varchar(20)) AS Pedido,
                    ISNULL(P.Nome, '') AS Cliente,
                    Pd.Status,
                    CONVERT(varchar(10), Pd.DataPedido, 103) AS DataPedido,
                    CONVERT(varchar(10), Pd.ValidoAte, 103) AS ValidoAte,
                    FORMAT(Pd.ValorTotal, 'N2', 'pt-BR') AS ValorTotal
                  FROM CorteCor_Pedido Pd
                  LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = Pd.IdPessoa
                  WHERE Pd.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR CAST(Pd.IdPedido AS varchar(20)) LIKE @Pesquisa OR ISNULL(P.Nome, '') LIKE @Pesquisa OR ISNULL(Pd.Observacoes, '') LIKE @Pesquisa)
                    AND (@IdPessoa IS NULL OR Pd.IdPessoa = @IdPessoa)
                    AND (@Status IS NULL OR Pd.Status = @Status)
                    AND (@DataInicio IS NULL OR CAST(Pd.DataPedido AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(Pd.DataPedido AS date) <= @DataFim)
                    AND (@SomenteVigentes = 0 OR (Pd.Status = 'Aberto' AND CAST(Pd.ValidoAte AS date) >= CAST(GETDATE() AS date)))
                  ORDER BY Pd.DataPedido DESC, Pd.IdPedido DESC;",
                Colunas("Pedido", "Cliente", "Status", "DataPedido", "ValidoAte", "ValorTotal")),

            RelatorioTipos.Vendas => (
                @"SELECT
                    CAST(V.IdVendaProduto AS varchar(20)) AS Venda,
                    ISNULL(P.Nome, '') AS Cliente,
                    V.Status,
                    CONVERT(varchar(10), V.DataVenda, 103) AS DataVenda,
                    FORMAT(V.ValorTotal, 'N2', 'pt-BR') AS ValorTotal,
                    ISNULL((
                        SELECT TOP 1 N.Status
                        FROM CorteCor_NotaFiscal N
                        WHERE N.IdSalao = V.IdSalao
                          AND N.IdVendaProduto = V.IdVendaProduto
                        ORDER BY N.DataEmissao DESC, N.DataAtualizacao DESC
                    ), '') AS StatusFiscal
                  FROM CorteCor_VendaProduto V
                  LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = V.IdPessoa
                  WHERE V.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR CAST(V.IdVendaProduto AS varchar(20)) LIKE @Pesquisa OR ISNULL(P.Nome, '') LIKE @Pesquisa OR ISNULL(V.Observacoes, '') LIKE @Pesquisa)
                    AND (@IdPessoa IS NULL OR V.IdPessoa = @IdPessoa)
                    AND (@Status IS NULL OR V.Status = @Status)
                    AND (@DataInicio IS NULL OR CAST(V.DataVenda AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(V.DataVenda AS date) <= @DataFim)
                  ORDER BY V.DataVenda DESC, V.IdVendaProduto DESC;",
                Colunas("Venda", "Cliente", "Status", "DataVenda", "ValorTotal", "StatusFiscal")),

            RelatorioTipos.Estoque => (
                @"SELECT
                    P.Nome AS Produto,
                    ISNULL(C.Nome, '') AS Categoria,
                    FORMAT(P.EstoqueAtual, 'N3', 'pt-BR') AS EstoqueAtual,
                    FORMAT(P.EstoqueMinimo, 'N3', 'pt-BR') AS EstoqueMinimo,
                    FORMAT(P.PrecoVenda, 'N2', 'pt-BR') AS PrecoVenda,
                    CASE
                        WHEN ISNULL(P.ControlarEstoque, 0) = 0 THEN 'Sem controle'
                        WHEN P.EstoqueAtual <= P.EstoqueMinimo THEN 'Atenção'
                        ELSE 'OK'
                    END AS Status
                  FROM CorteCor_Produto P
                  LEFT JOIN CorteCor_CategoriaProduto C ON C.IdCategoria = P.IdCategoria
                  WHERE P.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR P.Nome LIKE @Pesquisa OR ISNULL(P.CodigoProprio, '') LIKE @Pesquisa)
                    AND (@IdCategoria IS NULL OR P.IdCategoria = @IdCategoria)
                    AND (@IdProduto IS NULL OR P.IdProduto = @IdProduto)
                    AND (@Status IS NULL OR
                        (@Status = 'OK' AND ISNULL(P.ControlarEstoque, 0) = 1 AND P.EstoqueAtual > P.EstoqueMinimo) OR
                        (@Status = 'Atenção' AND ISNULL(P.ControlarEstoque, 0) = 1 AND P.EstoqueAtual <= P.EstoqueMinimo) OR
                        (@Status = 'Sem controle' AND ISNULL(P.ControlarEstoque, 0) = 0))
                  ORDER BY P.Nome;",
                Colunas("Produto", "Categoria", "EstoqueAtual", "EstoqueMinimo", "PrecoVenda", "Status")),

            RelatorioTipos.Agendamentos => (
                @"SELECT
                    CONVERT(varchar(16), A.DataHora, 103) AS DataHora,
                    ISNULL(P.Nome, '') AS Cliente,
                    ISNULL(S.Nome, '') AS Servico,
                    ISNULL(F.Nome, '') AS Funcionario,
                    A.Status
                  FROM CorteCor_Agendamento A
                  INNER JOIN CorteCor_Servico S ON S.IdServico = A.IdServico
                  LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = A.IdPessoa
                  LEFT JOIN CorteCor_Funcionario F ON F.IdFuncionario = A.IdFuncionario
                  WHERE S.IdSalao = @IdSalao
                    AND (@Status IS NULL OR A.Status = @Status)
                    AND (@IdPessoa IS NULL OR A.IdPessoa = @IdPessoa)
                    AND (@IdServico IS NULL OR A.IdServico = @IdServico)
                    AND (@IdFuncionario IS NULL OR A.IdFuncionario = @IdFuncionario)
                    AND (@DataInicio IS NULL OR CAST(A.DataHora AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(A.DataHora AS date) <= @DataFim)
                  ORDER BY A.DataHora DESC;",
                Colunas("DataHora", "Cliente", "Servico", "Funcionario", "Status")),

            RelatorioTipos.ModelosEmail => (
                @"SELECT
                    TipoEvento,
                    Assunto,
                    CASE WHEN ISNULL(Ativo, 0) = 1 THEN 'Ativo' ELSE 'Inativo' END AS Situacao,
                    CONVERT(varchar(10), DataAtualizacao, 103) AS Atualizacao
                  FROM CorteCor_ModeloEmail
                  WHERE IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR TipoEvento LIKE @Pesquisa OR Assunto LIKE @Pesquisa OR CorpoHTML LIKE @Pesquisa)
                    AND (@Tipo IS NULL OR TipoEvento = @Tipo)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(Ativo, 0) = 1 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(Ativo, 0) = 0 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY TipoEvento;",
                Colunas("TipoEvento", "Assunto", "Situacao", "Atualizacao")),

            RelatorioTipos.ModelosSms => (
                @"SELECT
                    TipoEvento,
                    Conteudo,
                    CASE WHEN ISNULL(Ativo, 0) = 1 THEN 'Ativo' ELSE 'Inativo' END AS Situacao,
                    CONVERT(varchar(10), DataAtualizacao, 103) AS Atualizacao
                  FROM CorteCor_ModeloSMS
                  WHERE IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR TipoEvento LIKE @Pesquisa OR Conteudo LIKE @Pesquisa)
                    AND (@Tipo IS NULL OR TipoEvento = @Tipo)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(Ativo, 0) = 1 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(Ativo, 0) = 0 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY TipoEvento;",
                Colunas("TipoEvento", "Conteudo", "Situacao", "Atualizacao")),

            RelatorioTipos.RegrasLembrete => (
                @"SELECT
                    TipoLembrete,
                    CONCAT(CAST(AntecedenciaValor AS varchar(10)), ' ', AntecedenciaUnidade) AS Antecedencia,
                    CONVERT(varchar(10), DataInicio, 103) AS Inicio,
                    CONVERT(varchar(10), DataFim, 103) AS Fim,
                    CASE WHEN ISNULL(Ativo, 0) = 1 THEN 'Ativa' ELSE 'Inativa' END AS Situacao
                  FROM CorteCor_LembreteConfig
                  WHERE IdSalao = @IdSalao
                    AND (@TipoLembrete IS NULL OR TipoLembrete = @TipoLembrete)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(Ativo, 0) = 1 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(Ativo, 0) = 0 THEN 1 ELSE 0 END END) = 1)
                    AND (@DataInicio IS NULL OR CAST(DataInicio AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(ISNULL(DataFim, DataInicio) AS date) <= @DataFim)
                  ORDER BY DataInicio DESC;",
                Colunas("TipoLembrete", "Antecedencia", "Inicio", "Fim", "Situacao")),

            RelatorioTipos.LogEnvios => (
                @"SELECT
                    CONVERT(varchar(16), DataEnvio, 103) AS DataEnvio,
                    ISNULL(TipoLembrete, 'Email') AS TipoLembrete,
                    CASE WHEN ISNULL(Destinatario, '') = '' THEN ISNULL(Telefone, '') ELSE Destinatario END AS Destino,
                    ISNULL(Assunto, '') AS Assunto,
                    Status,
                    ISNULL(MensagemErro, '') AS Mensagem
                  FROM CorteCor_LogEnvioEmail
                  WHERE (@Pesquisa IS NULL OR ISNULL(Destinatario, '') LIKE @Pesquisa OR ISNULL(Telefone, '') LIKE @Pesquisa OR ISNULL(Assunto, '') LIKE @Pesquisa OR ISNULL(MensagemErro, '') LIKE @Pesquisa)
                    AND (@Status IS NULL OR Status = @Status)
                    AND (@TipoLembrete IS NULL OR ISNULL(TipoLembrete, 'Email') = @TipoLembrete)
                    AND (@DataInicio IS NULL OR CAST(DataEnvio AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(DataEnvio AS date) <= @DataFim)
                  ORDER BY DataEnvio DESC;",
                Colunas("DataEnvio", "TipoLembrete", "Destino", "Assunto", "Status", "Mensagem")),

            RelatorioTipos.LogsSistema => (
                @"SELECT
                    CONVERT(varchar(16), DataHora, 103) AS DataHora,
                    TipoEvento,
                    LEFT(ISNULL(Mensagem, ''), 160) AS Mensagem,
                    ISNULL(CodigoErro, '') AS CodigoErro,
                    ISNULL([Usuario], '') AS Usuario
                  FROM CorteCor_NotaFiscalLog
                  WHERE IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR ISNULL(Mensagem, '') LIKE @Pesquisa OR ISNULL(TipoEvento, '') LIKE @Pesquisa)
                    AND (@Usuario IS NULL OR ISNULL([Usuario], '') LIKE @Usuario)
                    AND (@CodigoErro IS NULL OR ISNULL(CodigoErro, '') LIKE @CodigoErro)
                    AND (@DataInicio IS NULL OR CAST(DataHora AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(DataHora AS date) <= @DataFim)
                  ORDER BY DataHora DESC;",
                Colunas("DataHora", "TipoEvento", "Mensagem", "CodigoErro", "Usuario")),

            RelatorioTipos.CrmTarefas => (
                @"SELECT
                    ISNULL(P.Nome, '') AS Cliente,
                    T.Titulo AS Titulo,
                    ISNULL(T.CanalSugerido, '') AS Canal,
                    T.Status,
                    CONVERT(varchar(10), T.DataVencimento, 103) AS Vencimento
                  FROM CorteCor_CrmTarefa T
                  LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
                  WHERE T.IdSalao = @IdSalao
                    AND (@IdPessoa IS NULL OR T.IdPessoa = @IdPessoa)
                    AND (@Status IS NULL OR T.Status = @Status)
                    AND (@Pesquisa IS NULL OR T.Titulo LIKE @Pesquisa OR ISNULL(T.Descricao, '') LIKE @Pesquisa)
                    AND (@DataInicio IS NULL OR CAST(T.DataVencimento AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(T.DataVencimento AS date) <= @DataFim)
                  ORDER BY T.DataVencimento, T.Titulo;",
                Colunas("Cliente", "Titulo", "Canal", "Status", "Vencimento")),

            RelatorioTipos.CrmOportunidades => (
                @"SELECT
                    ISNULL(P.Nome, '') AS Cliente,
                    O.Titulo AS Titulo,
                    ISNULL(E.Nome, '') AS Etapa,
                    O.Status,
                    FORMAT(O.ValorEstimado, 'N2', 'pt-BR') AS ValorEstimado,
                    CONVERT(varchar(10), O.PrevisaoFechamento, 103) AS Previsao
                  FROM CorteCor_CrmOportunidade O
                  LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = O.IdPessoa
                  LEFT JOIN CorteCor_CrmEtapaFunil E ON E.IdEtapa = O.IdEtapa
                  WHERE O.IdSalao = @IdSalao
                    AND (@IdPessoa IS NULL OR O.IdPessoa = @IdPessoa)
                    AND (@Status IS NULL OR O.Status = @Status)
                    AND (@DataInicio IS NULL OR CAST(O.PrevisaoFechamento AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(O.PrevisaoFechamento AS date) <= @DataFim)
                  ORDER BY O.DataAtualizacao DESC;",
                Colunas("Cliente", "Titulo", "Etapa", "Status", "ValorEstimado", "Previsao")),

            RelatorioTipos.CrmCampanhas => (
                @"SELECT
                    Nome,
                    Canal,
                    Segmento,
                    Status,
                    CAST(TotalDestinatarios AS varchar(20)) AS Destinatarios,
                    CONVERT(varchar(10), DataCriacao, 103) AS Criacao
                  FROM CorteCor_CrmCampanha
                  WHERE IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa OR ISNULL(Assunto, '') LIKE @Pesquisa OR Conteudo LIKE @Pesquisa)
                    AND (@Canal IS NULL OR Canal = @Canal)
                    AND (@Segmento IS NULL OR Segmento = @Segmento)
                    AND (@Status IS NULL OR Status = @Status)
                    AND (@DataInicio IS NULL OR CAST(DataCriacao AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(DataCriacao AS date) <= @DataFim)
                  ORDER BY DataCriacao DESC;",
                Colunas("Nome", "Canal", "Segmento", "Status", "Destinatarios", "Criacao")),

            RelatorioTipos.FinanceiroPagarReceber => (
                @"SELECT
                    T.Tipo,
                    ISNULL(P.Nome, '') AS Pessoa,
                    ISNULL(PC.Descricao, '') AS Plano,
                    ISNULL(CC.Nome, '') AS Conta,
                    T.Status,
                    FORMAT(T.ValorOriginal, 'N2', 'pt-BR') AS Valor,
                    CONVERT(varchar(10), T.DataVencimento, 103) AS Vencimento
                  FROM CorteCor_FinanceiroTitulo T
                  LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
                  LEFT JOIN CorteCor_PlanoContas PC ON PC.IdPlano = T.IdPlano
                  LEFT JOIN CorteCor_ContaCaixa CC ON CC.IdConta = T.IdConta
                  WHERE T.IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR T.Descricao LIKE @Pesquisa OR ISNULL(P.Nome, '') LIKE @Pesquisa OR ISNULL(T.Documento, '') LIKE @Pesquisa)
                    AND (@Tipo IS NULL OR T.Tipo = @Tipo)
                    AND (@Status IS NULL OR T.Status = @Status)
                    AND (@IdPlano IS NULL OR T.IdPlano = @IdPlano)
                    AND (@IdConta IS NULL OR T.IdConta = @IdConta)
                    AND (@DataInicio IS NULL OR CAST(T.DataVencimento AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(T.DataVencimento AS date) <= @DataFim)
                  ORDER BY T.DataVencimento, T.Descricao;",
                Colunas("Tipo", "Pessoa", "Plano", "Conta", "Status", "Valor", "Vencimento")),

            RelatorioTipos.FinanceiroRelatorios => (
                @"SELECT
                    T.Tipo,
                    T.Status,
                    CAST(COUNT(1) AS varchar(20)) AS Quantidade,
                    FORMAT(SUM(T.ValorOriginal), 'N2', 'pt-BR') AS ValorOriginal,
                    FORMAT(SUM(T.ValorLiquidado), 'N2', 'pt-BR') AS ValorLiquidado,
                    FORMAT(SUM(T.ValorAberto), 'N2', 'pt-BR') AS ValorAberto
                  FROM CorteCor_FinanceiroTitulo T
                  WHERE T.IdSalao = @IdSalao
                    AND (@Tipo IS NULL OR T.Tipo = @Tipo)
                    AND (@Status IS NULL OR T.Status = @Status)
                    AND (@IdPlano IS NULL OR T.IdPlano = @IdPlano)
                    AND (@IdConta IS NULL OR T.IdConta = @IdConta)
                    AND (@DataInicio IS NULL OR CAST(T.DataVencimento AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(T.DataVencimento AS date) <= @DataFim)
                  GROUP BY T.Tipo, T.Status
                  ORDER BY T.Tipo, T.Status;",
                Colunas("Tipo", "Status", "Quantidade", "ValorOriginal", "ValorLiquidado", "ValorAberto")),

            RelatorioTipos.FinanceiroPlanoContas => (
                @"SELECT
                    ISNULL(Codigo, '') AS Codigo,
                    Descricao,
                    Tipo,
                    CASE WHEN ISNULL(Ativo, 1) = 1 THEN 'Ativo' ELSE 'Inativo' END AS Situacao
                  FROM CorteCor_PlanoContas
                  WHERE IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR ISNULL(Codigo, '') LIKE @Pesquisa OR Descricao LIKE @Pesquisa)
                    AND (@Tipo IS NULL OR Tipo = @Tipo)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(Ativo, 1) = 1 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(Ativo, 1) = 0 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY Descricao;",
                Colunas("Codigo", "Descricao", "Tipo", "Situacao")),

            RelatorioTipos.FinanceiroContasCaixa => (
                @"SELECT
                    Nome,
                    ISNULL(Tipo, '') AS Tipo,
                    ISNULL(Banco, '') AS Banco,
                    ISNULL(Agencia, '') AS Agencia,
                    FORMAT(SaldoInicial, 'N2', 'pt-BR') AS SaldoInicial,
                    CASE WHEN ISNULL(Ativo, 1) = 1 THEN 'Ativa' ELSE 'Inativa' END AS Situacao
                  FROM CorteCor_ContaCaixa
                  WHERE IdSalao = @IdSalao
                    AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa OR ISNULL(Banco, '') LIKE @Pesquisa OR ISNULL(Conta, '') LIKE @Pesquisa)
                    AND (@Tipo IS NULL OR Tipo = @Tipo)
                    AND (@Ativo IS NULL OR (CASE WHEN @Ativo = 1 THEN CASE WHEN ISNULL(Ativo, 1) = 1 THEN 1 ELSE 0 END ELSE CASE WHEN ISNULL(Ativo, 1) = 0 THEN 1 ELSE 0 END END) = 1)
                  ORDER BY Nome;",
                Colunas("Nome", "Tipo", "Banco", "Agencia", "SaldoInicial", "Situacao")),

            RelatorioTipos.PagamentosPendencias => (
                @"SELECT
                    CONVERT(varchar(10), ISNULL(Pag.CriadoEm, Pag.PagoEm), 103) AS Data,
                    ISNULL(Pe.Nome, '') AS Cliente,
                    ISNULL(Pag.Tipo, '') AS Meio,
                    Pag.Status,
                    FORMAT(Pag.Valor, 'N2', 'pt-BR') AS Valor
                  FROM CorteCor_Pagamento Pag
                  INNER JOIN CorteCor_Agendamento A ON A.IdAgendamento = Pag.IdAgendamento
                  INNER JOIN CorteCor_Servico S ON S.IdServico = A.IdServico
                  LEFT JOIN CorteCor_Pessoa Pe ON Pe.IdPessoa = A.IdPessoa
                  WHERE S.IdSalao = @IdSalao
                    AND (@IdPessoa IS NULL OR A.IdPessoa = @IdPessoa)
                    AND (@Status IS NULL OR Pag.Status = @Status)
                    AND (@DataInicio IS NULL OR CAST(ISNULL(Pag.CriadoEm, Pag.PagoEm) AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(ISNULL(Pag.CriadoEm, Pag.PagoEm) AS date) <= @DataFim)
                  ORDER BY ISNULL(Pag.CriadoEm, Pag.PagoEm) DESC;",
                Colunas("Data", "Cliente", "Meio", "Status", "Valor")),

            RelatorioTipos.ConfiguracoesSistema => (
                @"SELECT
                    ISNULL(S.Nome, '') AS Empresa,
                    ISNULL(S.Email, '') AS Email,
                    ISNULL(CFG.Cnpj, '') AS Cnpj,
                    CASE WHEN ISNULL(CFG.Ambiente, 1) = 2 THEN 'Homologação' ELSE 'Produção' END AS Ambiente,
                    CASE WHEN ISNULL(CFG.EmissaoAutomatica, 0) = 1 THEN 'Sim' ELSE 'Não' END AS EmissaoAutomatica,
                    CONVERT(varchar(10), CFG.DataAtualizacao, 103) AS Atualizacao
                  FROM CorteCor_Salao S
                  LEFT JOIN CorteCor_SalaoConfigFiscal CFG ON CFG.IdSalao = S.IdSalao
                  WHERE S.IdSalao = @IdSalao
                    AND (@Ambiente IS NULL OR CFG.Ambiente = @Ambiente)
                    AND (@EmissaoAutomatica IS NULL OR CFG.EmissaoAutomatica = @EmissaoAutomatica)
                  ORDER BY S.Nome;",
                Colunas("Empresa", "Email", "Cnpj", "Ambiente", "EmissaoAutomatica", "Atualizacao")),

            RelatorioTipos.NotasFiscais => (
                @"SELECT
                    TipoNota,
                    CAST(Numero AS varchar(20)) AS Numero,
                    CAST(Serie AS varchar(20)) AS Serie,
                    CASE WHEN Ambiente = 2 THEN 'Homologação' ELSE 'Produção' END AS Ambiente,
                    Status,
                    FORMAT(ValorTotal, 'N2', 'pt-BR') AS ValorTotal,
                    CONVERT(varchar(16), DataEmissao, 103) AS DataEmissao
                  FROM CorteCor_NotaFiscal
                  WHERE IdSalao = @IdSalao
                    AND (@Status IS NULL OR Status = @Status)
                    AND (@Ambiente IS NULL OR Ambiente = @Ambiente)
                    AND (@DataInicio IS NULL OR CAST(DataEmissao AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(DataEmissao AS date) <= @DataFim)
                  ORDER BY DataEmissao DESC, Numero DESC;",
                Colunas("TipoNota", "Numero", "Serie", "Ambiente", "Status", "ValorTotal", "DataEmissao")),

            RelatorioTipos.AuditoriaFiscal => (
                @"SELECT
                    CONVERT(varchar(16), DataHora, 103) AS DataHora,
                    ISNULL(TipoEvento, '') AS TipoEvento,
                    LEFT(ISNULL(Mensagem, ''), 160) AS Mensagem,
                    ISNULL(CodigoErro, '') AS CodigoErro,
                    ISNULL([Usuario], '') AS Usuario
                  FROM CorteCor_NotaFiscalLog
                  WHERE IdSalao = @IdSalao
                    AND (@DataInicio IS NULL OR CAST(DataHora AS date) >= @DataInicio)
                    AND (@DataFim IS NULL OR CAST(DataHora AS date) <= @DataFim)
                    AND (@CodigoErro IS NULL OR ISNULL(CodigoErro, '') LIKE @CodigoErro)
                    AND (@Usuario IS NULL OR ISNULL([Usuario], '') LIKE @Usuario)
                  ORDER BY DataHora DESC;",
                Colunas("DataHora", "TipoEvento", "Mensagem", "CodigoErro", "Usuario")),

            RelatorioTipos.DiagnosticoCertificado => (
                @"SELECT
                    ISNULL(S.Nome, '') AS Empresa,
                    CASE WHEN ISNULL(CFG.Ambiente, 1) = 2 THEN 'Homologação' ELSE 'Produção' END AS Ambiente,
                    ISNULL(CFG.Cnpj, '') AS Cnpj,
                    CONVERT(varchar(10), CFG.CertificadoValidade, 103) AS ValidadeCertificado,
                    CASE WHEN ISNULL(CFG.EmissaoAutomatica, 0) = 1 THEN 'Sim' ELSE 'Não' END AS EmissaoAutomatica,
                    CASE WHEN CFG.CertificadoPfx IS NULL THEN 'Sem certificado' ELSE 'Certificado informado' END AS Diagnostico
                  FROM CorteCor_Salao S
                  LEFT JOIN CorteCor_SalaoConfigFiscal CFG ON CFG.IdSalao = S.IdSalao
                  WHERE S.IdSalao = @IdSalao
                    AND (@Ambiente IS NULL OR CFG.Ambiente = @Ambiente)
                    AND (@EmissaoAutomatica IS NULL OR CFG.EmissaoAutomatica = @EmissaoAutomatica)
                  ORDER BY S.Nome;",
                Colunas("Empresa", "Ambiente", "Cnpj", "ValidadeCertificado", "EmissaoAutomatica", "Diagnostico")),

            _ => (
                "SELECT 'Relatório não encontrado.' AS Mensagem;",
                Colunas("Mensagem"))
        };
    }
}
