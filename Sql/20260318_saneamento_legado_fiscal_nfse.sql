SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    PRINT 'Iniciando saneamento legado do fluxo fiscal NFS-e...';

    UPDATE nf
       SET nf.ProtocoloAutorizacao = NULL,
           nf.DataAtualizacao = GETDATE()
    FROM dbo.CorteCor_NotaFiscal nf
    WHERE nf.ProtocoloAutorizacao IS NOT NULL
      AND (
            nf.ProtocoloAutorizacao LIKE '%E2406%'
         OR nf.ProtocoloAutorizacao LIKE '%E0840%'
         OR nf.ProtocoloAutorizacao LIKE '%chave de acesso consultada%'
         OR nf.ProtocoloAutorizacao LIKE '%Sistema Nacional NFS-e%'
         OR nf.ProtocoloAutorizacao LIKE '%evento de cancelamento%'
         OR nf.ProtocoloAutorizacao LIKE '%impedindo sua recepcao%'
         OR nf.ProtocoloAutorizacao LIKE '%impedindo sua recepção%'
         OR nf.ProtocoloAutorizacao LIKE '%Vazio%'
      );

    ;WITH EventosCancelamento AS
    (
        SELECT
            e.IdNotaFiscal,
            e.Status,
            e.XmlRetorno,
            e.DataRegistro,
            ROW_NUMBER() OVER (
                PARTITION BY e.IdNotaFiscal
                ORDER BY e.DataRegistro DESC, e.IdEvento DESC
            ) AS rn
        FROM dbo.CorteCor_NotaFiscalEvento e
        WHERE e.TipoEvento = 'Cancelamento NFS-e Nacional'
          AND (
                e.Status LIKE '%Autorizado%'
             OR e.Status LIKE '%E0840%'
             OR e.XmlRetorno LIKE '%Autorizado%'
             OR e.XmlRetorno LIKE '%E0840%'
             OR e.XmlRetorno LIKE '%evento de Cancelamento de NFS-e ja esta vinculado%'
             OR e.XmlRetorno LIKE '%evento de Cancelamento de NFS-e já está vinculado%'
          )
    )
    UPDATE nf
       SET nf.Status = 'Cancelada',
           nf.JustificativaRejeicao = NULL,
           nf.XmlRetorno = COALESCE(ec.XmlRetorno, nf.XmlRetorno),
           nf.DataAtualizacao = GETDATE()
    FROM dbo.CorteCor_NotaFiscal nf
    INNER JOIN EventosCancelamento ec
            ON ec.IdNotaFiscal = nf.IdNotaFiscal
           AND ec.rn = 1
    WHERE nf.TipoNota = 'NFS-e'
      AND nf.Status <> 'Cancelada';

    UPDATE nf
       SET nf.Status = 'Autorizada',
           nf.JustificativaRejeicao = NULL,
           nf.DataAtualizacao = GETDATE()
    FROM dbo.CorteCor_NotaFiscal nf
    WHERE nf.TipoNota = 'NFS-e'
      AND nf.Status <> 'Cancelada'
      AND (
            nf.XmlRetorno LIKE '%<cStat>100</cStat>%'
         OR nf.XmlRetorno LIKE '%Autorizada%'
      );

    COMMIT TRAN;

    PRINT 'Saneamento legado do fluxo fiscal NFS-e concluido com sucesso.';

    SELECT
        TipoNota,
        Status,
        COUNT(*) AS Quantidade
    FROM dbo.CorteCor_NotaFiscal
    WHERE TipoNota = 'NFS-e'
    GROUP BY TipoNota, Status
    ORDER BY Status;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @Mensagem NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @Numero INT = ERROR_NUMBER();
    DECLARE @Linha INT = ERROR_LINE();

    RAISERROR(
        'Falha no saneamento legado NFS-e. Numero: %d. Linha: %d. Mensagem: %s',
        16,
        1,
        @Numero,
        @Linha,
        @Mensagem
    );
END CATCH;
