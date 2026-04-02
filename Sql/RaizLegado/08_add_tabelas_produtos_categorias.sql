-- Fase 1: Categoria de Produtos
CREATE TABLE CorteCor_CategoriaProduto (
    IdCategoria INT IDENTITY(1,1) PRIMARY KEY,
    IdSalao INT NOT NULL,
    Nome VARCHAR(150) NOT NULL,
    Ativo BIT NOT NULL DEFAULT 1,
    DataCadastro DATETIME NOT NULL DEFAULT GETDATE()
);

-- Fase 2: Entidade Produtos
CREATE TABLE CorteCor_Produto (
    IdProduto INT IDENTITY(1,1) PRIMARY KEY,
    IdSalao INT NOT NULL,
    Nome VARCHAR(200) NOT NULL,
    CodigoProprio VARCHAR(50),
    IdCategoria INT,
    Tags VARCHAR(MAX),
    TipoUso VARCHAR(50),
    Arquivado BIT NOT NULL DEFAULT 0,
    Anotacoes VARCHAR(MAX),
    
    PrecoCusto DECIMAL(18,2),
    PrecoVenda DECIMAL(18,2) NOT NULL,
    MargemContribuicao DECIMAL(5,2),
    
    ControlarEstoque BIT NOT NULL DEFAULT 0,
    EstoqueAtual DECIMAL(18,3) DEFAULT 0,
    EstoqueMinimo DECIMAL(18,3) DEFAULT 0,
    
    Origem INT,
    ReferenciaEAN VARCHAR(50),
    PesoLiquido DECIMAL(18,3),
    PesoBruto DECIMAL(18,3),
    NCM VARCHAR(20),
    CEST VARCHAR(20),
    UnidadeComercial VARCHAR(10),
    ExcecaoIPI INT,
    CodBeneficioFiscalUF VARCHAR(20),
    
    UnidadeTributadaDiferente BIT NOT NULL DEFAULT 0,
    EANTributada VARCHAR(50),
    UnidadeTributada VARCHAR(10),
    QuantidadeTributada DECIMAL(18,3),
    IgnorarTribPrecoVenda BIT NOT NULL DEFAULT 0,
    AnotacoesFiscaisNFe VARCHAR(MAX),
    
    GrupoTributarioVinculado INT,
    
    DataCadastro DATETIME NOT NULL DEFAULT GETDATE(),
    Excluido BIT NOT NULL DEFAULT 0
);

-- Fase 2: Atualização Serviços
ALTER TABLE CorteCor_Servico ADD Tags VARCHAR(MAX);
ALTER TABLE CorteCor_Servico ADD Anotacoes VARCHAR(MAX);
ALTER TABLE CorteCor_Servico ADD ItemListaServicoLC116 VARCHAR(10);
ALTER TABLE CorteCor_Servico ADD IdCnae VARCHAR(50);
ALTER TABLE CorteCor_Servico ADD CodTributacaoNacional VARCHAR(50);
ALTER TABLE CorteCor_Servico ADD CodNBS VARCHAR(50);
ALTER TABLE CorteCor_Servico ADD Arquivado BIT NOT NULL DEFAULT 0;
