SET NOCOUNT ON;

IF OBJECT_ID('dbo.CorteCor_PlanoContas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_PlanoContas
    (
        IdPlano INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CorteCor_PlanoContas PRIMARY KEY,
        IdSalao INT NOT NULL,
        Codigo NVARCHAR(30) NULL,
        Descricao NVARCHAR(160) NOT NULL,
        Tipo CHAR(1) NOT NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_CorteCor_PlanoContas_Ativo DEFAULT(1)
    );
END;

IF OBJECT_ID('dbo.CorteCor_ContaCaixa', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_ContaCaixa
    (
        IdConta INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CorteCor_ContaCaixa PRIMARY KEY,
        IdSalao INT NOT NULL,
        Nome NVARCHAR(120) NOT NULL,
        Tipo NVARCHAR(40) NULL,
        Banco NVARCHAR(120) NULL,
        Agencia NVARCHAR(40) NULL,
        Conta NVARCHAR(40) NULL,
        SaldoInicial DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_ContaCaixa_SaldoInicial DEFAULT(0),
        Ativo BIT NOT NULL CONSTRAINT DF_CorteCor_ContaCaixa_Ativo DEFAULT(1)
    );
END;

IF OBJECT_ID('dbo.CorteCor_FinanceiroTitulo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_FinanceiroTitulo
    (
        IdTitulo UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CorteCor_FinanceiroTitulo PRIMARY KEY,
        IdSalao INT NOT NULL,
        Tipo NVARCHAR(10) NOT NULL,
        Origem NVARCHAR(40) NOT NULL,
        IdPessoa INT NULL,
        IdAgendamento INT NULL,
        IdPagamento UNIQUEIDENTIFIER NULL,
        IdPlano INT NULL,
        IdConta INT NULL,
        Descricao NVARCHAR(160) NOT NULL,
        Documento NVARCHAR(60) NULL,
        Status NVARCHAR(20) NOT NULL,
        ValorOriginal DECIMAL(18,2) NOT NULL,
        ValorLiquidado DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_FinanceiroTitulo_ValorLiquidado DEFAULT(0),
        ValorAberto DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_FinanceiroTitulo_ValorAberto DEFAULT(0),
        DataCompetencia DATE NOT NULL,
        DataVencimento DATE NOT NULL,
        DataLiquidacao DATETIME NULL,
        Conciliado BIT NOT NULL CONSTRAINT DF_CorteCor_FinanceiroTitulo_Conciliado DEFAULT(0),
        Observacoes NVARCHAR(MAX) NULL,
        DataCriacao DATETIME NOT NULL CONSTRAINT DF_CorteCor_FinanceiroTitulo_DataCriacao DEFAULT(GETDATE()),
        DataAtualizacao DATETIME NOT NULL CONSTRAINT DF_CorteCor_FinanceiroTitulo_DataAtualizacao DEFAULT(GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_FinanceiroTitulo_Salao_Status_Vencimento' AND object_id = OBJECT_ID('dbo.CorteCor_FinanceiroTitulo'))
BEGIN
    CREATE INDEX IX_CorteCor_FinanceiroTitulo_Salao_Status_Vencimento
        ON dbo.CorteCor_FinanceiroTitulo (IdSalao, Status, DataVencimento);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_FinanceiroTitulo_Salao_Tipo_Liquidacao' AND object_id = OBJECT_ID('dbo.CorteCor_FinanceiroTitulo'))
BEGIN
    CREATE INDEX IX_CorteCor_FinanceiroTitulo_Salao_Tipo_Liquidacao
        ON dbo.CorteCor_FinanceiroTitulo (IdSalao, Tipo, DataLiquidacao);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CorteCor_FinanceiroTitulo_Salao_Pagamento' AND object_id = OBJECT_ID('dbo.CorteCor_FinanceiroTitulo'))
BEGIN
    CREATE UNIQUE INDEX UX_CorteCor_FinanceiroTitulo_Salao_Pagamento
        ON dbo.CorteCor_FinanceiroTitulo (IdSalao, IdPagamento)
        WHERE IdPagamento IS NOT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_PlanoContas_Salao_Tipo' AND object_id = OBJECT_ID('dbo.CorteCor_PlanoContas'))
BEGIN
    CREATE INDEX IX_CorteCor_PlanoContas_Salao_Tipo
        ON dbo.CorteCor_PlanoContas (IdSalao, Tipo, Ativo);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_ContaCaixa_Salao_Ativo' AND object_id = OBJECT_ID('dbo.CorteCor_ContaCaixa'))
BEGIN
    CREATE INDEX IX_CorteCor_ContaCaixa_Salao_Ativo
        ON dbo.CorteCor_ContaCaixa (IdSalao, Ativo);
END;
