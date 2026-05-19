-- Estrutura complementar para compras, suporte e vínculos financeiros.
-- Pode ser executado no MySQL da DigitalOcean/Locaweb sem apagar dados existentes.

CREATE TABLE IF NOT EXISTS CorteCor_Compra (
    IdCompra INT NOT NULL AUTO_INCREMENT,
    IdSalao INT NOT NULL,
    IdPessoaFornecedor INT NULL,
    IdPlano INT NULL,
    IdConta INT NULL,
    Status VARCHAR(40) NOT NULL DEFAULT 'Lancada',
    Recorrencia VARCHAR(20) NOT NULL DEFAULT 'Nenhuma',
    PagaNaHora TINYINT(1) NOT NULL DEFAULT 0,
    ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    Documento VARCHAR(60) NULL,
    Observacoes VARCHAR(1000) NULL,
    UsuarioOperador VARCHAR(160) NULL,
    IdTituloFinanceiro CHAR(36) NULL,
    DataCompra DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataVencimento DATE NOT NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (IdCompra),
    INDEX IX_CorteCor_Compra_Salao_Data (IdSalao, DataCompra),
    INDEX IX_CorteCor_Compra_Fornecedor (IdPessoaFornecedor),
    INDEX IX_CorteCor_Compra_Titulo (IdTituloFinanceiro)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS CorteCor_CompraItem (
    IdCompraItem INT NOT NULL AUTO_INCREMENT,
    IdCompra INT NOT NULL,
    IdSalao INT NOT NULL,
    IdProduto INT NOT NULL,
    NomeProduto VARCHAR(160) NOT NULL,
    Quantidade DECIMAL(18,3) NOT NULL,
    ValorUnitario DECIMAL(18,2) NOT NULL,
    ValorTotal DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (IdCompraItem),
    INDEX IX_CorteCor_CompraItem_Compra (IdSalao, IdCompra),
    INDEX IX_CorteCor_CompraItem_Produto (IdProduto),
    CONSTRAINT FK_CorteCor_CompraItem_Compra
        FOREIGN KEY (IdCompra) REFERENCES CorteCor_Compra (IdCompra)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS CorteCor_SuporteChamado (
    IdChamado CHAR(36) NOT NULL,
    IdSalao INT NOT NULL,
    NomeUsuario VARCHAR(160) NULL,
    EmailUsuario VARCHAR(160) NULL,
    Mensagem VARCHAR(4000) NOT NULL,
    UrlOrigem VARCHAR(500) NULL,
    Status VARCHAR(80) NOT NULL DEFAULT 'Aberto',
    ErroEmail VARCHAR(1000) NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataAtualizacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (IdChamado),
    INDEX IX_CorteCor_SuporteChamado_Salao_Data (IdSalao, DataCriacao),
    INDEX IX_CorteCor_SuporteChamado_Status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

