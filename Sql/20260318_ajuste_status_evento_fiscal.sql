IF OBJECT_ID(N'dbo.CorteCor_NotaFiscalEvento', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.CorteCor_NotaFiscalEvento')
          AND name = 'Status'
          AND max_length < 100
    )
    BEGIN
        ALTER TABLE dbo.CorteCor_NotaFiscalEvento
        ALTER COLUMN Status VARCHAR(100) NOT NULL;
    END
END
GO
