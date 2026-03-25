SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.CorteCor_FinanceiroTitulo', 'IdVendaProduto') IS NULL
BEGIN
    ALTER TABLE dbo.CorteCor_FinanceiroTitulo
        ADD IdVendaProduto INT NULL;
END;
GO

IF OBJECT_ID('dbo.CorteCor_VendaProduto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_VendaProduto
    (
        IdVendaProduto INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdPessoa INT NULL,
        IdMeioPagamento INT NULL,
        Status NVARCHAR(40) NOT NULL,
        TipoPagamento NVARCHAR(80) NULL,
        RecebidoNaHora BIT NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_RecebidoNaHora DEFAULT(1),
        SolicitarEmissaoFiscalServico BIT NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_SolicitarEmissaoFiscalServico DEFAULT(0),
        SubtotalProdutos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_SubtotalProdutos DEFAULT(0),
        SubtotalServicos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_SubtotalServicos DEFAULT(0),
        Desconto DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_Desconto DEFAULT(0),
        Acrescimo DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_Acrescimo DEFAULT(0),
        ValorTotal DECIMAL(18,2) NOT NULL,
        Observacoes NVARCHAR(1000) NULL,
        Origem NVARCHAR(40) NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_Origem DEFAULT('Manual'),
        UsuarioOperador NVARCHAR(160) NULL,
        DataVenda DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_DataVenda DEFAULT(SYSDATETIME()),
        DataCriacao DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_DataCriacao DEFAULT(SYSDATETIME()),
        DataAtualizacao DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_VendaProduto_DataAtualizacao DEFAULT(SYSDATETIME())
    );
END;
GO

IF OBJECT_ID('dbo.CorteCor_VendaProdutoItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_VendaProdutoItem
    (
        IdItemVenda INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdVendaProduto INT NOT NULL,
        IdSalao INT NOT NULL,
        TipoItem NVARCHAR(20) NOT NULL,
        IdProduto INT NULL,
        IdServico INT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Quantidade DECIMAL(18,3) NOT NULL,
        ValorUnitario DECIMAL(18,2) NOT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Unidade NVARCHAR(10) NOT NULL CONSTRAINT DF_CorteCor_VendaProdutoItem_Unidade DEFAULT('UN'),
        ControlaEstoque BIT NOT NULL CONSTRAINT DF_CorteCor_VendaProdutoItem_ControlaEstoque DEFAULT(0),
        CodigoTributacaoMunicipio NVARCHAR(20) NULL,
        AliquotaIss DECIMAL(8,2) NULL,
        Ncm NVARCHAR(20) NULL,
        Cfop NVARCHAR(10) NULL
    );

    ALTER TABLE dbo.CorteCor_VendaProdutoItem
        ADD CONSTRAINT FK_CorteCor_VendaProdutoItem_Venda
            FOREIGN KEY (IdVendaProduto) REFERENCES dbo.CorteCor_VendaProduto(IdVendaProduto);
END;
GO

IF OBJECT_ID('dbo.CorteCor_MovimentoEstoque', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_MovimentoEstoque
    (
        IdMovimento UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdProduto INT NOT NULL,
        IdVendaProduto INT NULL,
        TipoMovimento NVARCHAR(20) NOT NULL,
        Origem NVARCHAR(40) NOT NULL,
        Quantidade DECIMAL(18,3) NOT NULL,
        SaldoAnterior DECIMAL(18,3) NOT NULL,
        SaldoPosterior DECIMAL(18,3) NOT NULL,
        Observacao NVARCHAR(500) NULL,
        UsuarioOperador NVARCHAR(160) NULL,
        DataMovimento DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_MovimentoEstoque_DataMovimento DEFAULT(SYSDATETIME())
    );

    ALTER TABLE dbo.CorteCor_MovimentoEstoque
        ADD CONSTRAINT FK_CorteCor_MovimentoEstoque_Produto
            FOREIGN KEY (IdProduto) REFERENCES dbo.CorteCor_Produto(IdProduto);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_VendaProduto_IdSalao_DataVenda' AND object_id = OBJECT_ID('dbo.CorteCor_VendaProduto'))
BEGIN
    CREATE INDEX IX_CorteCor_VendaProduto_IdSalao_DataVenda
        ON dbo.CorteCor_VendaProduto (IdSalao, DataVenda DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_VendaProdutoItem_IdVendaProduto' AND object_id = OBJECT_ID('dbo.CorteCor_VendaProdutoItem'))
BEGIN
    CREATE INDEX IX_CorteCor_VendaProdutoItem_IdVendaProduto
        ON dbo.CorteCor_VendaProdutoItem (IdVendaProduto);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_MovimentoEstoque_IdSalao_DataMovimento' AND object_id = OBJECT_ID('dbo.CorteCor_MovimentoEstoque'))
BEGIN
    CREATE INDEX IX_CorteCor_MovimentoEstoque_IdSalao_DataMovimento
        ON dbo.CorteCor_MovimentoEstoque (IdSalao, DataMovimento DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_FinanceiroTitulo_IdVendaProduto' AND object_id = OBJECT_ID('dbo.CorteCor_FinanceiroTitulo'))
BEGIN
    CREATE INDEX IX_CorteCor_FinanceiroTitulo_IdVendaProduto
        ON dbo.CorteCor_FinanceiroTitulo (IdSalao, IdVendaProduto);
END;
GO
