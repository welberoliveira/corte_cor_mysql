SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.CorteCor_Pedido', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_Pedido
    (
        IdPedido INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdPessoa INT NULL,
        IdMeioPagamento INT NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_CorteCor_Pedido_Status DEFAULT('Aberto'),
        TipoPagamento NVARCHAR(80) NULL,
        ValidoAte DATE NOT NULL,
        SubtotalProdutos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_Pedido_SubtotalProdutos DEFAULT(0),
        SubtotalServicos DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_Pedido_SubtotalServicos DEFAULT(0),
        Desconto DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_Pedido_Desconto DEFAULT(0),
        Acrescimo DECIMAL(18,2) NOT NULL CONSTRAINT DF_CorteCor_Pedido_Acrescimo DEFAULT(0),
        ValorTotal DECIMAL(18,2) NOT NULL,
        Observacoes NVARCHAR(1000) NULL,
        Origem NVARCHAR(40) NOT NULL CONSTRAINT DF_CorteCor_Pedido_Origem DEFAULT('Manual'),
        UsuarioOperador NVARCHAR(160) NULL,
        IdVendaProduto INT NULL,
        DataPedido DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_Pedido_DataPedido DEFAULT(SYSDATETIME()),
        DataCriacao DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_Pedido_DataCriacao DEFAULT(SYSDATETIME()),
        DataAtualizacao DATETIME2 NOT NULL CONSTRAINT DF_CorteCor_Pedido_DataAtualizacao DEFAULT(SYSDATETIME())
    );
END;
GO

IF OBJECT_ID('dbo.CorteCor_PedidoItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_PedidoItem
    (
        IdItemPedido INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdPedido INT NOT NULL,
        IdSalao INT NOT NULL,
        TipoItem NVARCHAR(20) NOT NULL,
        IdProduto INT NULL,
        IdServico INT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Quantidade DECIMAL(18,3) NOT NULL,
        ValorUnitario DECIMAL(18,2) NOT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Unidade NVARCHAR(10) NOT NULL CONSTRAINT DF_CorteCor_PedidoItem_Unidade DEFAULT('UN'),
        ControlaEstoque BIT NOT NULL CONSTRAINT DF_CorteCor_PedidoItem_ControlaEstoque DEFAULT(0),
        CodigoTributacaoMunicipio NVARCHAR(20) NULL,
        AliquotaIss DECIMAL(8,2) NULL,
        Ncm NVARCHAR(20) NULL,
        Cfop NVARCHAR(10) NULL
    );

    ALTER TABLE dbo.CorteCor_PedidoItem
        ADD CONSTRAINT FK_CorteCor_PedidoItem_Pedido
            FOREIGN KEY (IdPedido) REFERENCES dbo.CorteCor_Pedido(IdPedido);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_Pedido_IdSalao_DataPedido' AND object_id = OBJECT_ID('dbo.CorteCor_Pedido'))
BEGIN
    CREATE INDEX IX_CorteCor_Pedido_IdSalao_DataPedido
        ON dbo.CorteCor_Pedido (IdSalao, DataPedido DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_Pedido_IdSalao_Status_ValidoAte' AND object_id = OBJECT_ID('dbo.CorteCor_Pedido'))
BEGIN
    CREATE INDEX IX_CorteCor_Pedido_IdSalao_Status_ValidoAte
        ON dbo.CorteCor_Pedido (IdSalao, Status, ValidoAte);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_Pedido_IdVendaProduto' AND object_id = OBJECT_ID('dbo.CorteCor_Pedido'))
BEGIN
    CREATE INDEX IX_CorteCor_Pedido_IdVendaProduto
        ON dbo.CorteCor_Pedido (IdSalao, IdVendaProduto);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_PedidoItem_IdPedido' AND object_id = OBJECT_ID('dbo.CorteCor_PedidoItem'))
BEGIN
    CREATE INDEX IX_CorteCor_PedidoItem_IdPedido
        ON dbo.CorteCor_PedidoItem (IdPedido);
END;
GO
