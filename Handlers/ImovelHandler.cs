using System.Data;
using System.Text;
using CorteCor.Models;
using Dapper;

namespace CorteCor.Handlers;

public class ImovelHandler
{
    private readonly IDatabaseHandler _databaseHandler;

    public ImovelHandler(IDatabaseHandler databaseHandler)
    {
        _databaseHandler = databaseHandler;
    }

    public PagedResult<Imovel> ListarPaginadoPorSalao(
        int idSalao,
        string? pesquisa,
        string? status,
        string? finalidade,
        string? tipoImovel,
        bool incluirInativos,
        int pageIndex,
        int pageSize)
    {
        var result = new PagedResult<Imovel>
        {
            PageIndex = pageIndex < 1 ? 1 : pageIndex,
            PageSize = pageSize < 1 ? 10 : pageSize
        };

        var where = new StringBuilder("WHERE I.IdSalao = @IdSalao AND COALESCE(I.Excluido, 0) = 0");
        var parameters = new DynamicParameters();
        parameters.Add("IdSalao", idSalao);

        if (!incluirInativos && !string.Equals(status, "Inativo", StringComparison.OrdinalIgnoreCase))
        {
            where.Append(" AND I.StatusAnuncio <> 'Inativo'");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Append(" AND I.StatusAnuncio = @Status");
            parameters.Add("Status", status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(finalidade))
        {
            where.Append(" AND I.Finalidade = @Finalidade");
            parameters.Add("Finalidade", finalidade.Trim());
        }

        if (!string.IsNullOrWhiteSpace(tipoImovel))
        {
            where.Append(" AND I.TipoImovel = @TipoImovel");
            parameters.Add("TipoImovel", tipoImovel.Trim());
        }

        if (!string.IsNullOrWhiteSpace(pesquisa))
        {
            where.Append(@"
 AND (
        I.CodigoImovel LIKE @Pesquisa
     OR I.Titulo LIKE @Pesquisa
     OR I.Bairro LIKE @Pesquisa
     OR I.Cidade LIKE @Pesquisa
     OR I.Estado LIKE @Pesquisa
     OR COALESCE(I.TagsInternas, '') LIKE @Pesquisa
 )");
            parameters.Add("Pesquisa", $"%{pesquisa.Trim()}%");
        }

        parameters.Add("PageSize", result.PageSize);
        parameters.Add("Offset", (result.PageIndex - 1) * result.PageSize);

        using var conn = _databaseHandler.GetConnection();

        result.TotalCount = conn.ExecuteScalar<int>($@"
SELECT COUNT(1)
FROM CorteCor_Imovel I
{where};", parameters);

        result.Items = conn.Query<Imovel>($@"
SELECT
    I.*,
    (SELECT F.CaminhoArquivo FROM CorteCor_ImovelFoto F WHERE F.IdImovel = I.IdImovel ORDER BY F.FotoCapa DESC, F.Ordem, F.IdFoto LIMIT 1) AS FotoCapaUrl,
    (SELECT COUNT(1) FROM CorteCor_ImovelFoto F WHERE F.IdImovel = I.IdImovel) AS QuantidadeFotos,
    (SELECT COUNT(1) FROM CorteCor_ImovelLead L WHERE L.IdImovel = I.IdImovel) AS QuantidadeLeads
FROM CorteCor_Imovel I
{where}
ORDER BY COALESCE(I.OrdemPrioridade, 999999), I.DataAtualizacao DESC, I.IdImovel DESC
LIMIT @PageSize OFFSET @Offset;", parameters).ToList();

        return result;
    }

    public PagedResult<Imovel> ListarWebPorSalao(int idSalao, ImovelWebFiltro filtro, int pageIndex, int pageSize)
    {
        filtro ??= new ImovelWebFiltro();

        var result = new PagedResult<Imovel>
        {
            PageIndex = pageIndex < 1 ? 1 : pageIndex,
            PageSize = pageSize < 1 ? 8 : pageSize
        };

        const string valorExpression = "CASE WHEN LOWER(I.Finalidade) = 'aluguel' THEN COALESCE(I.ValorAluguel, 0) ELSE COALESCE(I.ValorVenda, 0) END";
        var where = new StringBuilder("WHERE I.IdSalao = @IdSalao AND COALESCE(I.Excluido, 0) = 0");
        var parameters = new DynamicParameters();
        parameters.Add("IdSalao", idSalao);

        if (filtro.SomentePublicados)
        {
            where.Append(" AND COALESCE(I.PublicarNoSite, 0) = 1");
        }

        if (filtro.SomenteDisponiveis)
        {
            where.Append(" AND COALESCE(I.ImovelDisponivel, 0) = 1");
        }

        if (!string.IsNullOrWhiteSpace(filtro.StatusAnuncio))
        {
            where.Append(" AND I.StatusAnuncio = @StatusAnuncio");
            parameters.Add("StatusAnuncio", filtro.StatusAnuncio.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filtro.Finalidade))
        {
            where.Append(" AND I.Finalidade = @Finalidade");
            parameters.Add("Finalidade", filtro.Finalidade.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filtro.TipoImovel))
        {
            where.Append(" AND I.TipoImovel = @TipoImovel");
            parameters.Add("TipoImovel", filtro.TipoImovel.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            where.Append(" AND I.Estado = @Estado");
            parameters.Add("Estado", filtro.Estado.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filtro.Cidade))
        {
            where.Append(" AND I.Cidade LIKE @Cidade");
            parameters.Add("Cidade", $"%{filtro.Cidade.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(filtro.Bairro))
        {
            where.Append(" AND I.Bairro LIKE @Bairro");
            parameters.Add("Bairro", $"%{filtro.Bairro.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(filtro.Pesquisa))
        {
            where.Append(@"
 AND (
        I.CodigoImovel LIKE @Pesquisa
     OR I.Titulo LIKE @Pesquisa
     OR I.Subtitulo LIKE @Pesquisa
     OR I.Bairro LIKE @Pesquisa
     OR I.Cidade LIKE @Pesquisa
     OR I.Estado LIKE @Pesquisa
     OR COALESCE(I.TagsInternas, '') LIKE @Pesquisa
 )");
            parameters.Add("Pesquisa", $"%{filtro.Pesquisa.Trim()}%");
        }

        if (filtro.ValorMinimo.HasValue)
        {
            where.Append($" AND {valorExpression} >= @ValorMinimo");
            parameters.Add("ValorMinimo", filtro.ValorMinimo.Value);
        }

        if (filtro.ValorMaximo.HasValue)
        {
            where.Append($" AND {valorExpression} <= @ValorMaximo");
            parameters.Add("ValorMaximo", filtro.ValorMaximo.Value);
        }

        if (filtro.AreaMinima.HasValue)
        {
            where.Append(" AND COALESCE(I.AreaConstruidaPrivativa, 0) >= @AreaMinima");
            parameters.Add("AreaMinima", filtro.AreaMinima.Value);
        }

        if (filtro.AreaMaxima.HasValue)
        {
            where.Append(" AND COALESCE(I.AreaConstruidaPrivativa, 0) <= @AreaMaxima");
            parameters.Add("AreaMaxima", filtro.AreaMaxima.Value);
        }

        if (filtro.QuartosMinimo.HasValue)
        {
            where.Append(" AND COALESCE(I.Quartos, 0) >= @QuartosMinimo");
            parameters.Add("QuartosMinimo", filtro.QuartosMinimo.Value);
        }

        if (filtro.SuitesMinimo.HasValue)
        {
            where.Append(" AND COALESCE(I.Suites, 0) >= @SuitesMinimo");
            parameters.Add("SuitesMinimo", filtro.SuitesMinimo.Value);
        }

        if (filtro.BanheirosMinimo.HasValue)
        {
            where.Append(" AND COALESCE(I.Banheiros, 0) >= @BanheirosMinimo");
            parameters.Add("BanheirosMinimo", filtro.BanheirosMinimo.Value);
        }

        if (filtro.VagasMinimo.HasValue)
        {
            where.Append(" AND COALESCE(I.VagasGaragem, 0) >= @VagasMinimo");
            parameters.Add("VagasMinimo", filtro.VagasMinimo.Value);
        }

        AddFlag(filtro.DestaqueNaBusca, "DestaqueNaBusca");
        AddFlag(filtro.PrecoSobConsulta, "PrecoSobConsulta");
        AddFlag(filtro.AceitaFinanciamento, "AceitaFinanciamento");
        AddFlag(filtro.AceitaPermuta, "AceitaPermuta");
        AddFlag(filtro.Piscina, "Piscina");
        AddFlag(filtro.ArCondicionado, "ArCondicionado");
        AddFlag(filtro.Churrasqueira, "Churrasqueira");
        AddFlag(filtro.Sauna, "Sauna");
        AddFlag(filtro.Jardim, "Jardim");
        AddFlag(filtro.AreaGourmet, "AreaGourmet");
        AddFlag(filtro.Jacuzzi, "Jacuzzi");
        AddFlag(filtro.Hidromassagem, "Hidromassagem");
        AddFlag(filtro.Escritorio, "Escritorio");
        AddFlag(filtro.SalaTV, "SalaTV");
        AddFlag(filtro.CozinhaPlanejada, "CozinhaPlanejada");
        AddFlag(filtro.Closet, "ClosetCaracteristica");
        AddFlag(filtro.Varanda, "VarandaCaracteristica");
        AddFlag(filtro.Lavabo, "LavaboCaracteristica");

        if (filtro.ComVideo)
        {
            where.Append(" AND COALESCE(I.VideoUrl, '') <> ''");
        }

        if (filtro.ComTourVirtual)
        {
            where.Append(" AND COALESCE(I.TourVirtualUrl, '') <> ''");
        }

        if (filtro.ComFotos)
        {
            where.Append(" AND EXISTS (SELECT 1 FROM CorteCor_ImovelFoto FX WHERE FX.IdImovel = I.IdImovel)");
        }

        var orderBy = filtro.Ordenacao switch
        {
            "valor_asc" => $"{valorExpression} ASC, I.IdImovel DESC",
            "valor_desc" => $"{valorExpression} DESC, I.IdImovel DESC",
            "area_desc" => "COALESCE(I.AreaConstruidaPrivativa, 0) DESC, I.IdImovel DESC",
            "mais_recentes" => "I.DataAtualizacao DESC, I.IdImovel DESC",
            _ => "COALESCE(I.DestaqueNaBusca, 0) DESC, COALESCE(I.OrdemPrioridade, 999999), I.DataAtualizacao DESC, I.IdImovel DESC"
        };

        parameters.Add("PageSize", result.PageSize);
        parameters.Add("Offset", (result.PageIndex - 1) * result.PageSize);

        using var conn = _databaseHandler.GetConnection();

        result.TotalCount = conn.ExecuteScalar<int>($@"
SELECT COUNT(1)
FROM CorteCor_Imovel I
{where};", parameters);

        result.Items = conn.Query<Imovel>($@"
SELECT
    I.*,
    (SELECT F.CaminhoArquivo FROM CorteCor_ImovelFoto F WHERE F.IdImovel = I.IdImovel ORDER BY F.FotoCapa DESC, F.Ordem, F.IdFoto LIMIT 1) AS FotoCapaUrl,
    (SELECT COUNT(1) FROM CorteCor_ImovelFoto F WHERE F.IdImovel = I.IdImovel) AS QuantidadeFotos,
    (SELECT COUNT(1) FROM CorteCor_ImovelLead L WHERE L.IdImovel = I.IdImovel) AS QuantidadeLeads
FROM CorteCor_Imovel I
{where}
ORDER BY {orderBy}
LIMIT @PageSize OFFSET @Offset;", parameters).ToList();

        return result;

        void AddFlag(bool enabled, string column)
        {
            if (enabled)
            {
                where.Append($" AND COALESCE(I.{column}, 0) = 1");
            }
        }
    }

    public int? ObterIdSalaoPadraoVitrine()
    {
        using var conn = _databaseHandler.GetConnection();
        return conn.QueryFirstOrDefault<int?>(@"
SELECT I.IdSalao
FROM CorteCor_Imovel I
WHERE COALESCE(I.Excluido, 0) = 0
  AND COALESCE(I.PublicarNoSite, 0) = 1
GROUP BY I.IdSalao
ORDER BY COUNT(1) DESC, MIN(I.IdImovel)
LIMIT 1;");
    }

    public Imovel? ObterPorIdESalao(int idImovel, int idSalao)
    {
        using var conn = _databaseHandler.GetConnection();
        var imovel = conn.QueryFirstOrDefault<Imovel>(@"
SELECT *
FROM CorteCor_Imovel
WHERE IdImovel = @IdImovel
  AND IdSalao = @IdSalao
  AND COALESCE(Excluido, 0) = 0;", new { IdImovel = idImovel, IdSalao = idSalao });

        if (imovel == null)
        {
            return null;
        }

        imovel.Fotos = ListarFotos(conn, idImovel);
        imovel.Leads = ListarLeads(conn, idImovel, 20);
        imovel.QuantidadeFotos = imovel.Fotos.Count;
        imovel.QuantidadeLeads = imovel.Leads.Count;
        return imovel;
    }

    public Imovel? ObterWebPorId(int idImovel, int? idSalao = null)
    {
        using var conn = _databaseHandler.GetConnection();
        var imovel = conn.QueryFirstOrDefault<Imovel>(@"
SELECT *
FROM CorteCor_Imovel
WHERE IdImovel = @IdImovel
  AND (@IdSalao IS NULL OR IdSalao = @IdSalao)
  AND COALESCE(Excluido, 0) = 0
  AND COALESCE(PublicarNoSite, 0) = 1
  AND COALESCE(ImovelDisponivel, 0) = 1;", new { IdImovel = idImovel, IdSalao = idSalao });

        if (imovel == null)
        {
            return null;
        }

        imovel.Fotos = ListarFotos(conn, idImovel);
        imovel.Leads = ListarLeads(conn, idImovel, 20);
        imovel.QuantidadeFotos = imovel.Fotos.Count;
        imovel.QuantidadeLeads = imovel.Leads.Count;
        return imovel;
    }

    public bool ExisteCodigoPorSalao(int idSalao, string codigoImovel, int? ignorarIdImovel = null)
    {
        using var conn = _databaseHandler.GetConnection();
        return conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_Imovel
WHERE IdSalao = @IdSalao
  AND CodigoImovel = @CodigoImovel
  AND COALESCE(Excluido, 0) = 0
  AND (@IgnorarIdImovel IS NULL OR IdImovel <> @IgnorarIdImovel);",
            new { IdSalao = idSalao, CodigoImovel = codigoImovel, IgnorarIdImovel = ignorarIdImovel }) > 0;
    }

    public int Salvar(Imovel imovel)
    {
        using var conn = _databaseHandler.GetConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            if (imovel.IdImovel > 0)
            {
                Atualizar(conn, tx, imovel);
            }
            else
            {
                imovel.IdImovel = Inserir(conn, tx, imovel);
            }

            tx.Commit();
            return imovel.IdImovel;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void Inativar(int idImovel, int idSalao)
    {
        using var conn = _databaseHandler.GetConnection();
        conn.Execute(@"
UPDATE CorteCor_Imovel
SET StatusAnuncio = 'Inativo',
    PublicarNoSite = 0,
    ImovelDisponivel = 0,
    DataAtualizacao = NOW()
WHERE IdImovel = @IdImovel
  AND IdSalao = @IdSalao
  AND COALESCE(Excluido, 0) = 0;", new { IdImovel = idImovel, IdSalao = idSalao });
    }

    public void Excluir(int idImovel, int idSalao)
    {
        using var conn = _databaseHandler.GetConnection();
        conn.Execute(@"
UPDATE CorteCor_Imovel
SET Excluido = 1,
    PublicarNoSite = 0,
    ImovelDisponivel = 0,
    DataAtualizacao = NOW()
WHERE IdImovel = @IdImovel
  AND IdSalao = @IdSalao;", new { IdImovel = idImovel, IdSalao = idSalao });
    }

    public List<ImovelFoto> ListarFotos(int idImovel)
    {
        using var conn = _databaseHandler.GetConnection();
        return ListarFotos(conn, idImovel);
    }

    public List<ImovelLead> ListarLeads(int idImovel, int limite = 20)
    {
        using var conn = _databaseHandler.GetConnection();
        return ListarLeads(conn, idImovel, limite);
    }

    public int AdicionarFoto(ImovelFoto foto)
    {
        using var conn = _databaseHandler.GetConnection();
        var id = conn.ExecuteScalar<int>(@"
INSERT INTO CorteCor_ImovelFoto
    (IdImovel, CaminhoArquivo, FotoCapa, Ordem, Legenda, AltText, DataCadastro)
VALUES
    (@IdImovel, @CaminhoArquivo, @FotoCapa, @Ordem, @Legenda, @AltText, NOW());
SELECT LAST_INSERT_ID();", foto);

        GarantirFotoCapa(foto.IdImovel);
        return id;
    }

    public void AtualizarFoto(ImovelFoto foto)
    {
        using var conn = _databaseHandler.GetConnection();
        conn.Execute(@"
UPDATE CorteCor_ImovelFoto
SET Ordem = @Ordem,
    Legenda = @Legenda,
    AltText = @AltText,
    FotoCapa = @FotoCapa
WHERE IdFoto = @IdFoto
  AND IdImovel = @IdImovel;", foto);

        if (foto.FotoCapa)
        {
            conn.Execute(@"
UPDATE CorteCor_ImovelFoto
SET FotoCapa = CASE WHEN IdFoto = @IdFoto THEN 1 ELSE 0 END
WHERE IdImovel = @IdImovel;", new { foto.IdFoto, foto.IdImovel });
        }

        GarantirFotoCapa(foto.IdImovel);
    }

    public void RemoverFoto(int idFoto, int idImovel)
    {
        using var conn = _databaseHandler.GetConnection();
        conn.Execute("DELETE FROM CorteCor_ImovelFoto WHERE IdFoto = @IdFoto AND IdImovel = @IdImovel;", new { IdFoto = idFoto, IdImovel = idImovel });
        GarantirFotoCapa(idImovel);
    }

    public int AdicionarLead(ImovelLead lead)
    {
        using var conn = _databaseHandler.GetConnection();
        return conn.ExecuteScalar<int>(@"
INSERT INTO CorteCor_ImovelLead
    (IdImovel, NomeInteressado, Email, TelefoneWhatsapp, Mensagem, AceiteTermos, AceitaReceberNovidades, Status, Origem, IpOrigem, UserAgent, DataCadastro)
VALUES
    (@IdImovel, @NomeInteressado, @Email, @TelefoneWhatsapp, @Mensagem, @AceiteTermos, @AceitaReceberNovidades, @Status, @Origem, @IpOrigem, @UserAgent, NOW());
SELECT LAST_INSERT_ID();", lead);
    }

    private static int Inserir(IDbConnection conn, IDbTransaction tx, Imovel imovel)
    {
        return conn.ExecuteScalar<int>(SqlInsert, imovel, tx);
    }

    private static void Atualizar(IDbConnection conn, IDbTransaction tx, Imovel imovel)
    {
        conn.Execute(SqlUpdate, imovel, tx);
    }

    private static List<ImovelFoto> ListarFotos(IDbConnection conn, int idImovel)
    {
        return conn.Query<ImovelFoto>(@"
SELECT *
FROM CorteCor_ImovelFoto
WHERE IdImovel = @IdImovel
ORDER BY FotoCapa DESC, Ordem, IdFoto;", new { IdImovel = idImovel }).ToList();
    }

    private static List<ImovelLead> ListarLeads(IDbConnection conn, int idImovel, int limite)
    {
        return conn.Query<ImovelLead>(@"
SELECT *
FROM CorteCor_ImovelLead
WHERE IdImovel = @IdImovel
ORDER BY DataCadastro DESC
LIMIT @Limite;", new { IdImovel = idImovel, Limite = limite }).ToList();
    }

    private void GarantirFotoCapa(int idImovel)
    {
        using var conn = _databaseHandler.GetConnection();
        var capaId = conn.ExecuteScalar<int?>(@"
SELECT IdFoto
FROM CorteCor_ImovelFoto
WHERE IdImovel = @IdImovel AND FotoCapa = 1
ORDER BY Ordem, IdFoto
LIMIT 1;", new { IdImovel = idImovel });

        if (capaId.HasValue)
        {
            conn.Execute(@"
UPDATE CorteCor_ImovelFoto
SET FotoCapa = CASE WHEN IdFoto = @IdFoto THEN 1 ELSE 0 END
WHERE IdImovel = @IdImovel;", new { IdFoto = capaId.Value, IdImovel = idImovel });
            return;
        }

        var primeiraFotoId = conn.ExecuteScalar<int?>(@"
SELECT IdFoto
FROM CorteCor_ImovelFoto
WHERE IdImovel = @IdImovel
ORDER BY Ordem, IdFoto
LIMIT 1;", new { IdImovel = idImovel });

        if (primeiraFotoId.HasValue)
        {
            conn.Execute(@"
UPDATE CorteCor_ImovelFoto
SET FotoCapa = CASE WHEN IdFoto = @IdFoto THEN 1 ELSE 0 END
WHERE IdImovel = @IdImovel;", new { IdFoto = primeiraFotoId.Value, IdImovel = idImovel });
        }
    }

    private const string SqlInsert = @"
INSERT INTO CorteCor_Imovel (
    IdSalao, CodigoImovel, StatusAnuncio, Finalidade, TipoImovel, Titulo, Subtitulo,
    ImobiliariaResponsavel, CreciResponsavelLegal, DataCadastro, DataAtualizacao,
    ValorVenda, ValorAluguel, ValorCondominio, ValorIPTU, PrecoSobConsulta, AceitaFinanciamento, AceitaPermuta,
    ObservacoesComerciais, AvisoAlteracaoPreco, Estado, Cidade, Bairro, Logradouro, Numero, Complemento, CEP,
    Latitude, Longitude, ExibirEnderecoCompleto, TextoReferenciaRegiao, AreaConstruidaPrivativa, AreaAproximada,
    AreaLoteTerreno, AreaLoteAproximada, Quartos, Suites, Banheiros, Lavabos, VagasGaragem, Salas, Varandas,
    Closets, Depositos, Piscina, ArCondicionado, Churrasqueira, Sauna, Jardim, DependenciaEmpregadaDCE,
    AreaGourmet, Jacuzzi, Hidromassagem, Escritorio, SalaTV, CozinhaPlanejada, ClosetCaracteristica,
    VarandaCaracteristica, LavaboCaracteristica, DescricaoPrincipal, ListaComposicao, DestaquesImovel,
    ObservacoesFinais, TextoDisclaimer, VideoUrl, TourVirtualUrl, PlantaArquivoUrl, NomeImobiliariaContato,
    TelefonePrincipal, WhatsApp, EmailContato, TextoBotaoWhatsApp, MensagemPadraoWhatsApp, PermitirVerTelefone,
    ReceberNovidades, TermosPrivacidadeTexto, SlugUrl, TituloSEO, MetaDescription, ImagemCompartilhamento,
    TextoCompartilhamento, PermitirCompartilhamento, PublicarNoSite, DestaqueNaBusca, TagsInternas, IndexarGoogle,
    ImovelDisponivel, OrdemPrioridade, OrigemCadastro, IdExterno, Excluido
) VALUES (
    @IdSalao, @CodigoImovel, @StatusAnuncio, @Finalidade, @TipoImovel, @Titulo, @Subtitulo,
    @ImobiliariaResponsavel, @CreciResponsavelLegal, @DataCadastro, @DataAtualizacao,
    @ValorVenda, @ValorAluguel, @ValorCondominio, @ValorIPTU, @PrecoSobConsulta, @AceitaFinanciamento, @AceitaPermuta,
    @ObservacoesComerciais, @AvisoAlteracaoPreco, @Estado, @Cidade, @Bairro, @Logradouro, @Numero, @Complemento, @CEP,
    @Latitude, @Longitude, @ExibirEnderecoCompleto, @TextoReferenciaRegiao, @AreaConstruidaPrivativa, @AreaAproximada,
    @AreaLoteTerreno, @AreaLoteAproximada, @Quartos, @Suites, @Banheiros, @Lavabos, @VagasGaragem, @Salas, @Varandas,
    @Closets, @Depositos, @Piscina, @ArCondicionado, @Churrasqueira, @Sauna, @Jardim, @DependenciaEmpregadaDCE,
    @AreaGourmet, @Jacuzzi, @Hidromassagem, @Escritorio, @SalaTV, @CozinhaPlanejada, @ClosetCaracteristica,
    @VarandaCaracteristica, @LavaboCaracteristica, @DescricaoPrincipal, @ListaComposicao, @DestaquesImovel,
    @ObservacoesFinais, @TextoDisclaimer, @VideoUrl, @TourVirtualUrl, @PlantaArquivoUrl, @NomeImobiliariaContato,
    @TelefonePrincipal, @WhatsApp, @EmailContato, @TextoBotaoWhatsApp, @MensagemPadraoWhatsApp, @PermitirVerTelefone,
    @ReceberNovidades, @TermosPrivacidadeTexto, @SlugUrl, @TituloSEO, @MetaDescription, @ImagemCompartilhamento,
    @TextoCompartilhamento, @PermitirCompartilhamento, @PublicarNoSite, @DestaqueNaBusca, @TagsInternas, @IndexarGoogle,
    @ImovelDisponivel, @OrdemPrioridade, @OrigemCadastro, @IdExterno, @Excluido
);
SELECT LAST_INSERT_ID();";

    private const string SqlUpdate = @"
UPDATE CorteCor_Imovel
SET CodigoImovel = @CodigoImovel,
    StatusAnuncio = @StatusAnuncio,
    Finalidade = @Finalidade,
    TipoImovel = @TipoImovel,
    Titulo = @Titulo,
    Subtitulo = @Subtitulo,
    ImobiliariaResponsavel = @ImobiliariaResponsavel,
    CreciResponsavelLegal = @CreciResponsavelLegal,
    DataCadastro = @DataCadastro,
    DataAtualizacao = @DataAtualizacao,
    ValorVenda = @ValorVenda,
    ValorAluguel = @ValorAluguel,
    ValorCondominio = @ValorCondominio,
    ValorIPTU = @ValorIPTU,
    PrecoSobConsulta = @PrecoSobConsulta,
    AceitaFinanciamento = @AceitaFinanciamento,
    AceitaPermuta = @AceitaPermuta,
    ObservacoesComerciais = @ObservacoesComerciais,
    AvisoAlteracaoPreco = @AvisoAlteracaoPreco,
    Estado = @Estado,
    Cidade = @Cidade,
    Bairro = @Bairro,
    Logradouro = @Logradouro,
    Numero = @Numero,
    Complemento = @Complemento,
    CEP = @CEP,
    Latitude = @Latitude,
    Longitude = @Longitude,
    ExibirEnderecoCompleto = @ExibirEnderecoCompleto,
    TextoReferenciaRegiao = @TextoReferenciaRegiao,
    AreaConstruidaPrivativa = @AreaConstruidaPrivativa,
    AreaAproximada = @AreaAproximada,
    AreaLoteTerreno = @AreaLoteTerreno,
    AreaLoteAproximada = @AreaLoteAproximada,
    Quartos = @Quartos,
    Suites = @Suites,
    Banheiros = @Banheiros,
    Lavabos = @Lavabos,
    VagasGaragem = @VagasGaragem,
    Salas = @Salas,
    Varandas = @Varandas,
    Closets = @Closets,
    Depositos = @Depositos,
    Piscina = @Piscina,
    ArCondicionado = @ArCondicionado,
    Churrasqueira = @Churrasqueira,
    Sauna = @Sauna,
    Jardim = @Jardim,
    DependenciaEmpregadaDCE = @DependenciaEmpregadaDCE,
    AreaGourmet = @AreaGourmet,
    Jacuzzi = @Jacuzzi,
    Hidromassagem = @Hidromassagem,
    Escritorio = @Escritorio,
    SalaTV = @SalaTV,
    CozinhaPlanejada = @CozinhaPlanejada,
    ClosetCaracteristica = @ClosetCaracteristica,
    VarandaCaracteristica = @VarandaCaracteristica,
    LavaboCaracteristica = @LavaboCaracteristica,
    DescricaoPrincipal = @DescricaoPrincipal,
    ListaComposicao = @ListaComposicao,
    DestaquesImovel = @DestaquesImovel,
    ObservacoesFinais = @ObservacoesFinais,
    TextoDisclaimer = @TextoDisclaimer,
    VideoUrl = @VideoUrl,
    TourVirtualUrl = @TourVirtualUrl,
    PlantaArquivoUrl = @PlantaArquivoUrl,
    NomeImobiliariaContato = @NomeImobiliariaContato,
    TelefonePrincipal = @TelefonePrincipal,
    WhatsApp = @WhatsApp,
    EmailContato = @EmailContato,
    TextoBotaoWhatsApp = @TextoBotaoWhatsApp,
    MensagemPadraoWhatsApp = @MensagemPadraoWhatsApp,
    PermitirVerTelefone = @PermitirVerTelefone,
    ReceberNovidades = @ReceberNovidades,
    TermosPrivacidadeTexto = @TermosPrivacidadeTexto,
    SlugUrl = @SlugUrl,
    TituloSEO = @TituloSEO,
    MetaDescription = @MetaDescription,
    ImagemCompartilhamento = @ImagemCompartilhamento,
    TextoCompartilhamento = @TextoCompartilhamento,
    PermitirCompartilhamento = @PermitirCompartilhamento,
    PublicarNoSite = @PublicarNoSite,
    DestaqueNaBusca = @DestaqueNaBusca,
    TagsInternas = @TagsInternas,
    IndexarGoogle = @IndexarGoogle,
    ImovelDisponivel = @ImovelDisponivel,
    OrdemPrioridade = @OrdemPrioridade,
    OrigemCadastro = @OrigemCadastro,
    IdExterno = @IdExterno,
    Excluido = @Excluido
WHERE IdImovel = @IdImovel
  AND IdSalao = @IdSalao;";
}
