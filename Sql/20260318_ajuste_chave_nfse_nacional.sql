IF OBJECT_ID(N'dbo.CorteCor_NotaFiscal', N'U') IS NULL
BEGIN
    RAISERROR('Tabela dbo.CorteCor_NotaFiscal nao encontrada.', 16, 1);
    RETURN;
END
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CorteCor_NotaFiscal')
      AND name = 'ChaveAcesso'
      AND max_length < 80
)
BEGIN
    ALTER TABLE dbo.CorteCor_NotaFiscal
    ALTER COLUMN ChaveAcesso VARCHAR(80) NULL;
END
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CorteCor_NotaFiscal')
      AND name = 'ChaveAcessoNacional'
      AND max_length < 80
)
BEGIN
    ALTER TABLE dbo.CorteCor_NotaFiscal
    ALTER COLUMN ChaveAcessoNacional VARCHAR(80) NULL;
END
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CorteCor_NotaFiscal')
      AND name = 'NumeroNFSeNacional'
      AND max_length < 50
)
BEGIN
    ALTER TABLE dbo.CorteCor_NotaFiscal
    ALTER COLUMN NumeroNFSeNacional VARCHAR(50) NULL;
END
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CorteCor_NotaFiscal')
      AND name = 'ProtocoloAutorizacao'
      AND max_length < 255
)
BEGIN
    ALTER TABLE dbo.CorteCor_NotaFiscal
    ALTER COLUMN ProtocoloAutorizacao VARCHAR(255) NULL;
END
GO

PRINT 'Ajuste de colunas fiscais da NFS-e aplicado com sucesso.';
GO

IF OBJECT_ID(N'dbo.CorteCor_NotaFiscalEvento', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.CorteCor_NotaFiscalEvento')
          AND name = 'ProtocoloEvento'
          AND max_length < 255
    )
    BEGIN
        ALTER TABLE dbo.CorteCor_NotaFiscalEvento
        ALTER COLUMN ProtocoloEvento VARCHAR(255) NULL;
    END
END
GO

PRINT 'Ajuste de ProtocoloEvento em CorteCor_NotaFiscalEvento aplicado com sucesso.';
GO
