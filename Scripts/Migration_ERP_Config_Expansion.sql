-- =============================================
-- ERP CONFIGURATION EXPANSION - MASTER SCRIPT
-- =============================================

-- 1. CONFIGURAÇÃO GERAL
CREATE TABLE CorteCor_ConfigGeral (
    IdSalao INT PRIMARY KEY,
    NomeFantasia NVARCHAR(200),
    LogoUrl NVARCHAR(500),
    TemaCor NVARCHAR(7) DEFAULT '#0d6efd',
    ModoPDV BIT DEFAULT 0,
    ModoEstoque BIT DEFAULT 0,
    AgendamentoOnline BIT DEFAULT 0,
    MinutosAntecedencia INT DEFAULT 0,
    DataAtualizacao DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_ConfigGeral_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);

-- 2. PLANO DE CONTAS
CREATE TABLE CorteCor_PlanoContas (
    IdPlano INT PRIMARY KEY IDENTITY(1,1),
    IdSalao INT NOT NULL,
    Codigo NVARCHAR(20), -- Ex: 1.01.002
    Descricao NVARCHAR(100) NOT NULL,
    Tipo CHAR(1) NOT NULL, -- 'R' (Receita), 'D' (Despesa)
    Ativo BIT DEFAULT 1,
    CONSTRAINT FK_PlanoContas_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);

-- 3. CONTAS CAIXA
CREATE TABLE CorteCor_ContaCaixa (
    IdConta INT PRIMARY KEY IDENTITY(1,1),
    IdSalao INT NOT NULL,
    Nome NVARCHAR(50) NOT NULL,
    Tipo NVARCHAR(20), -- 'Banco', 'Caixa Fisico'
    Banco NVARCHAR(50),
    Agencia NVARCHAR(10),
    Conta NVARCHAR(20),
    SaldoInicial DECIMAL(18,2) DEFAULT 0,
    Ativo BIT DEFAULT 1,
    CONSTRAINT FK_ContaCaixa_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);

-- 4. CONFIGURAÇÃO PIX
CREATE TABLE CorteCor_ConfigPix (
    IdSalao INT PRIMARY KEY,
    ChavePix NVARCHAR(100),
    PSP NVARCHAR(50), -- MercadoPago, Efí, Inter, PagSeguro
    ClientId NVARCHAR(255),
    ClientSecret NVARCHAR(255),
    Certificado VARBINARY(MAX),
    Ativo BIT DEFAULT 0,
    CONSTRAINT FK_ConfigPix_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);

-- 5. API / APLICATIVOS
CREATE TABLE CorteCor_ConfigApi (
    IdApi INT PRIMARY KEY IDENTITY(1,1),
    IdSalao INT NOT NULL,
    NomeApp NVARCHAR(50),
    ApiKey UNIQUEIDENTIFIER DEFAULT NEWID(),
    DataCriacao DATETIME DEFAULT GETDATE(),
    UltimoAcesso DATETIME,
    Ativo BIT DEFAULT 1,
    CONSTRAINT FK_ConfigApi_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);

GO
