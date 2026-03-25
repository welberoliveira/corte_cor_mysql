SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.CorteCor_VendaProdutoItem', 'QuantidadeCancelada') IS NULL
BEGIN
    ALTER TABLE dbo.CorteCor_VendaProdutoItem
        ADD QuantidadeCancelada DECIMAL(18,3) NOT NULL CONSTRAINT DF_CorteCor_VendaProdutoItem_QuantidadeCancelada DEFAULT(0);
END;
GO

IF COL_LENGTH('dbo.CorteCor_VendaProdutoItem', 'QuantidadeDevolvida') IS NULL
BEGIN
    ALTER TABLE dbo.CorteCor_VendaProdutoItem
        ADD QuantidadeDevolvida DECIMAL(18,3) NOT NULL CONSTRAINT DF_CorteCor_VendaProdutoItem_QuantidadeDevolvida DEFAULT(0);
END;
GO

IF COL_LENGTH('dbo.CorteCor_VendaProdutoItem', 'QuantidadeTrocada') IS NULL
BEGIN
    ALTER TABLE dbo.CorteCor_VendaProdutoItem
        ADD QuantidadeTrocada DECIMAL(18,3) NOT NULL CONSTRAINT DF_CorteCor_VendaProdutoItem_QuantidadeTrocada DEFAULT(0);
END;
GO

IF OBJECT_ID('dbo.CorteCor_VendaPosVenda', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_VendaPosVenda
    (
        IdPosVenda INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdVendaProduto INT NOT NULL,
        TipoOperacao NVARCHAR(30) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_CorteCor_VendaPosVenda_Status DEFAULT('Processada'),
        ValorCredito DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_VendaPosVenda_ValorCredito DEFAULT(0),
        ValorReposicao DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_VendaPosVenda_ValorReposicao DEFAULT(0),
        DiferencaFinanceira DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_VendaPosVenda_DiferencaFinanceira DEFAULT(0),
        Observacoes NVARCHAR(1000) NULL,
        UsuarioOperador NVARCHAR(160) NULL,
        DataOperacao DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_VendaPosVenda_DataOperacao DEFAULT(SYSDATETIME())
    );

    ALTER TABLE dbo.CorteCor_VendaPosVenda
        ADD CONSTRAINT FK_CorteCor_VendaPosVenda_Venda
            FOREIGN KEY (IdVendaProduto) REFERENCES dbo.CorteCor_VendaProduto(IdVendaProduto);
END;
GO

IF OBJECT_ID('dbo.CorteCor_VendaPosVendaItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_VendaPosVendaItem
    (
        IdPosVendaItem INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPosVenda INT NOT NULL,
        IdSalao INT NOT NULL,
        IdVendaProduto INT NOT NULL,
        IdItemVenda INT NULL,
        TipoRegistro NVARCHAR(20) NOT NULL,
        TipoItem NVARCHAR(20) NOT NULL,
        IdProduto INT NULL,
        IdServico INT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Quantidade DECIMAL(18,3) NOT NULL,
        ValorUnitario DECIMAL(18,2) NOT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Unidade NVARCHAR(10) NOT NULL CONSTRAINT DF_CorteCor_VendaPosVendaItem_Unidade DEFAULT('UN'),
        ControlaEstoque BIT NOT NULL CONSTRAINT DF_CorteCor_VendaPosVendaItem_ControlaEstoque DEFAULT(0)
    );

    ALTER TABLE dbo.CorteCor_VendaPosVendaItem
        ADD CONSTRAINT FK_CorteCor_VendaPosVendaItem_PosVenda
            FOREIGN KEY (IdPosVenda) REFERENCES dbo.CorteCor_VendaPosVenda(IdPosVenda);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_VendaPosVenda_IdSalao_IdVendaProduto' AND object_id = OBJECT_ID('dbo.CorteCor_VendaPosVenda'))
BEGIN
    CREATE INDEX IX_CorteCor_VendaPosVenda_IdSalao_IdVendaProduto
        ON dbo.CorteCor_VendaPosVenda (IdSalao, IdVendaProduto, DataOperacao DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_VendaPosVendaItem_IdPosVenda' AND object_id = OBJECT_ID('dbo.CorteCor_VendaPosVendaItem'))
BEGIN
    CREATE INDEX IX_CorteCor_VendaPosVendaItem_IdPosVenda
        ON dbo.CorteCor_VendaPosVendaItem (IdPosVenda);
END;
GO
