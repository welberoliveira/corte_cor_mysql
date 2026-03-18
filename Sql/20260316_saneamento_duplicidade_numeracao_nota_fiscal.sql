SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/*
Objetivo:
1. Identificar numeracoes duplicadas em dbo.CorteCor_NotaFiscal.
2. Preservar o registro "principal" de cada grupo duplicado.
3. Renumerar os registros excedentes com novos numeros sequenciais.
4. Criar o indice unico UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero.

Salvaguardas:
- O script ABORTA se encontrar mais de um registro fiscalmente consolidado
  no mesmo grupo duplicado.
- Considera "fiscalmente consolidado" quando houver qualquer um dos sinais:
  Status em ('Autorizada', 'Cancelada', 'Inutilizada')
  ou ChaveAcesso preenchida
  ou ChaveAcessoNacional preenchida
  ou ProtocoloAutorizacao preenchido
  ou NumeroNFSeNacional preenchido
  ou NumeroRecibo preenchido

Recomendacao:
- Execute primeiro em homologacao.
- Revise o resultado final emitido nos SELECTs do final do script.
*/
GO

IF OBJECT_ID(N'dbo.CorteCor_NotaFiscal', N'U') IS NULL
BEGIN
    RAISERROR('Tabela dbo.CorteCor_NotaFiscal nao encontrada.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID('tempdb..#Duplicados') IS NOT NULL DROP TABLE #Duplicados;
IF OBJECT_ID('tempdb..#RankingDuplicados') IS NOT NULL DROP TABLE #RankingDuplicados;
IF OBJECT_ID('tempdb..#GruposCriticos') IS NOT NULL DROP TABLE #GruposCriticos;
IF OBJECT_ID('tempdb..#Renumeracoes') IS NOT NULL DROP TABLE #Renumeracoes;
GO

SELECT
    nf.IdSalao,
    nf.TipoNota,
    nf.Ambiente,
    nf.Serie,
    nf.Numero,
    COUNT(*) AS Quantidade
INTO #Duplicados
FROM dbo.CorteCor_NotaFiscal nf
GROUP BY
    nf.IdSalao,
    nf.TipoNota,
    nf.Ambiente,
    nf.Serie,
    nf.Numero
HAVING COUNT(*) > 1;
GO

IF NOT EXISTS (SELECT 1 FROM #Duplicados)
BEGIN
    PRINT 'Nenhuma duplicidade de numeracao foi encontrada em dbo.CorteCor_NotaFiscal.';

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero'
          AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero
            ON dbo.CorteCor_NotaFiscal(IdSalao, TipoNota, Ambiente, Serie, Numero);
        PRINT 'Indice unico UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero criado com sucesso.';
    END
    ELSE
    BEGIN
        PRINT 'Indice unico UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero ja existe.';
    END

    RETURN;
END
GO

SELECT
    nf.IdNotaFiscal,
    nf.IdSalao,
    nf.TipoNota,
    nf.Ambiente,
    nf.Serie,
    nf.Numero,
    nf.Status,
    nf.ChaveAcesso,
    nf.ChaveAcessoNacional,
    nf.NumeroNFSeNacional,
    nf.NumeroRecibo,
    nf.ProtocoloAutorizacao,
    nf.DataEmissao,
    nf.DataAtualizacao,
    CASE
        WHEN nf.Status IN ('Autorizada', 'Cancelada', 'Inutilizada')
          OR NULLIF(LTRIM(RTRIM(ISNULL(nf.ChaveAcesso, ''))), '') IS NOT NULL
          OR NULLIF(LTRIM(RTRIM(ISNULL(nf.ChaveAcessoNacional, ''))), '') IS NOT NULL
          OR NULLIF(LTRIM(RTRIM(ISNULL(nf.ProtocoloAutorizacao, ''))), '') IS NOT NULL
          OR NULLIF(LTRIM(RTRIM(ISNULL(nf.NumeroNFSeNacional, ''))), '') IS NOT NULL
          OR NULLIF(LTRIM(RTRIM(ISNULL(nf.NumeroRecibo, ''))), '') IS NOT NULL
        THEN 1
        ELSE 0
    END AS EhConsolidada,
    ROW_NUMBER() OVER (
        PARTITION BY nf.IdSalao, nf.TipoNota, nf.Ambiente, nf.Serie, nf.Numero
        ORDER BY
            CASE
                WHEN nf.Status IN ('Autorizada', 'Cancelada', 'Inutilizada') THEN 0
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(nf.ChaveAcesso, ''))), '') IS NOT NULL THEN 1
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(nf.ChaveAcessoNacional, ''))), '') IS NOT NULL THEN 2
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(nf.ProtocoloAutorizacao, ''))), '') IS NOT NULL THEN 3
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(nf.NumeroNFSeNacional, ''))), '') IS NOT NULL THEN 4
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(nf.NumeroRecibo, ''))), '') IS NOT NULL THEN 5
                WHEN nf.Status = 'Processando' THEN 6
                WHEN nf.Status = 'Pendente' THEN 7
                WHEN nf.Status = 'Rejeitada' THEN 8
                ELSE 9
            END,
            nf.DataEmissao,
            nf.DataAtualizacao,
            nf.IdNotaFiscal
    ) AS OrdemNoGrupo
INTO #RankingDuplicados
FROM dbo.CorteCor_NotaFiscal nf
INNER JOIN #Duplicados d
    ON d.IdSalao = nf.IdSalao
   AND d.TipoNota = nf.TipoNota
   AND d.Ambiente = nf.Ambiente
   AND d.Serie = nf.Serie
   AND d.Numero = nf.Numero;
GO

SELECT
    rd.IdSalao,
    rd.TipoNota,
    rd.Ambiente,
    rd.Serie,
    rd.Numero,
    SUM(CASE WHEN rd.EhConsolidada = 1 THEN 1 ELSE 0 END) AS ConsolidadaNoGrupo,
    COUNT(*) AS TotalNoGrupo
INTO #GruposCriticos
FROM #RankingDuplicados rd
GROUP BY
    rd.IdSalao,
    rd.TipoNota,
    rd.Ambiente,
    rd.Serie,
    rd.Numero
HAVING SUM(CASE WHEN rd.EhConsolidada = 1 THEN 1 ELSE 0 END) > 1;
GO

IF EXISTS (SELECT 1 FROM #GruposCriticos)
BEGIN
    PRINT 'Foram encontrados grupos com mais de um registro fiscalmente consolidado.';
    PRINT 'O script foi interrompido para evitar renumeracao insegura.';

    SELECT
        gc.IdSalao,
        gc.TipoNota,
        gc.Ambiente,
        gc.Serie,
        gc.Numero,
        gc.ConsolidadaNoGrupo,
        gc.TotalNoGrupo
    FROM #GruposCriticos gc
    ORDER BY
        gc.IdSalao,
        gc.TipoNota,
        gc.Ambiente,
        gc.Serie,
        gc.Numero;

    SELECT
        rd.IdNotaFiscal,
        rd.IdSalao,
        rd.TipoNota,
        rd.Ambiente,
        rd.Serie,
        rd.Numero,
        rd.Status,
        rd.ChaveAcesso,
        rd.ChaveAcessoNacional,
        rd.NumeroNFSeNacional,
        rd.NumeroRecibo,
        rd.ProtocoloAutorizacao,
        rd.DataEmissao,
        rd.DataAtualizacao,
        rd.EhConsolidada,
        rd.OrdemNoGrupo
    FROM #RankingDuplicados rd
    INNER JOIN #GruposCriticos gc
        ON gc.IdSalao = rd.IdSalao
       AND gc.TipoNota = rd.TipoNota
       AND gc.Ambiente = rd.Ambiente
       AND gc.Serie = rd.Serie
       AND gc.Numero = rd.Numero
    ORDER BY
        rd.IdSalao,
        rd.TipoNota,
        rd.Ambiente,
        rd.Serie,
        rd.Numero,
        rd.OrdemNoGrupo;

    RAISERROR('Saneamento abortado: existe mais de uma nota consolidada no mesmo numero fiscal.', 16, 1);
    RETURN;
END
GO

CREATE TABLE #Renumeracoes
(
    IdNotaFiscal UNIQUEIDENTIFIER NOT NULL,
    IdSalao INT NOT NULL,
    TipoNota VARCHAR(20) NOT NULL,
    Ambiente INT NOT NULL,
    Serie INT NOT NULL,
    NumeroAnterior INT NOT NULL,
    NumeroNovo INT NOT NULL,
    Status VARCHAR(100) NULL,
    DataEmissao DATETIME NULL
);
GO

BEGIN TRANSACTION;

DECLARE
    @IdSalao INT,
    @TipoNota VARCHAR(20),
    @Ambiente INT,
    @Serie INT,
    @NumeroDuplicado INT,
    @MaxNumeroAtual INT;

DECLARE duplicidade_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT
    d.IdSalao,
    d.TipoNota,
    d.Ambiente,
    d.Serie,
    d.Numero
FROM #Duplicados d
ORDER BY
    d.IdSalao,
    d.TipoNota,
    d.Ambiente,
    d.Serie,
    d.Numero;

OPEN duplicidade_cursor;

FETCH NEXT FROM duplicidade_cursor
INTO @IdSalao, @TipoNota, @Ambiente, @Serie, @NumeroDuplicado;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT
        @MaxNumeroAtual = ISNULL(MAX(nf.Numero), 0)
    FROM dbo.CorteCor_NotaFiscal nf WITH (UPDLOCK, HOLDLOCK)
    WHERE nf.IdSalao = @IdSalao
      AND nf.TipoNota = @TipoNota
      AND nf.Ambiente = @Ambiente
      AND nf.Serie = @Serie;

    ;WITH Excedentes AS
    (
        SELECT
            rd.IdNotaFiscal,
            rd.IdSalao,
            rd.TipoNota,
            rd.Ambiente,
            rd.Serie,
            rd.Numero,
            rd.Status,
            rd.DataEmissao,
            ROW_NUMBER() OVER (
                ORDER BY rd.OrdemNoGrupo, rd.DataEmissao, rd.DataAtualizacao, rd.IdNotaFiscal
            ) AS SequenciaExcedente
        FROM #RankingDuplicados rd
        WHERE rd.IdSalao = @IdSalao
          AND rd.TipoNota = @TipoNota
          AND rd.Ambiente = @Ambiente
          AND rd.Serie = @Serie
          AND rd.Numero = @NumeroDuplicado
          AND rd.OrdemNoGrupo > 1
    )
    UPDATE nf
       SET nf.Numero = @MaxNumeroAtual + e.SequenciaExcedente,
           nf.DataAtualizacao = GETDATE()
    OUTPUT
        inserted.IdNotaFiscal,
        inserted.IdSalao,
        inserted.TipoNota,
        inserted.Ambiente,
        inserted.Serie,
        deleted.Numero,
        inserted.Numero,
        inserted.Status,
        inserted.DataEmissao
    INTO #Renumeracoes
    (
        IdNotaFiscal,
        IdSalao,
        TipoNota,
        Ambiente,
        Serie,
        NumeroAnterior,
        NumeroNovo,
        Status,
        DataEmissao
    )
    FROM dbo.CorteCor_NotaFiscal nf
    INNER JOIN Excedentes e
        ON e.IdNotaFiscal = nf.IdNotaFiscal;

    FETCH NEXT FROM duplicidade_cursor
    INTO @IdSalao, @TipoNota, @Ambiente, @Serie, @NumeroDuplicado;
END

CLOSE duplicidade_cursor;
DEALLOCATE duplicidade_cursor;

IF EXISTS
(
    SELECT
        nf.IdSalao,
        nf.TipoNota,
        nf.Ambiente,
        nf.Serie,
        nf.Numero
    FROM dbo.CorteCor_NotaFiscal nf
    GROUP BY
        nf.IdSalao,
        nf.TipoNota,
        nf.Ambiente,
        nf.Serie,
        nf.Numero
    HAVING COUNT(*) > 1
)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Ainda existem numeracoes duplicadas apos a tentativa de saneamento. Nenhuma alteracao foi mantida.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero'
      AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal')
)
BEGIN
    CREATE UNIQUE INDEX UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero
        ON dbo.CorteCor_NotaFiscal(IdSalao, TipoNota, Ambiente, Serie, Numero);
END

COMMIT TRANSACTION;
GO

PRINT 'Saneamento concluido.';
PRINT 'Revise os registros renumerados abaixo.';
GO

SELECT
    r.IdSalao,
    r.TipoNota,
    r.Ambiente,
    r.Serie,
    r.NumeroAnterior,
    r.NumeroNovo,
    r.IdNotaFiscal,
    r.Status,
    r.DataEmissao
FROM #Renumeracoes r
ORDER BY
    r.IdSalao,
    r.TipoNota,
    r.Ambiente,
    r.Serie,
    r.NumeroNovo;
GO

SELECT
    nf.IdSalao,
    nf.TipoNota,
    nf.Ambiente,
    nf.Serie,
    MIN(nf.Numero) AS MenorNumero,
    MAX(nf.Numero) AS MaiorNumero,
    COUNT(*) AS QuantidadeRegistros
FROM dbo.CorteCor_NotaFiscal nf
GROUP BY
    nf.IdSalao,
    nf.TipoNota,
    nf.Ambiente,
    nf.Serie
ORDER BY
    nf.IdSalao,
    nf.TipoNota,
    nf.Ambiente,
    nf.Serie;
GO

SELECT
    i.name AS NomeIndice,
    i.is_unique AS EhUnico
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal')
  AND i.name = 'UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero';
GO

