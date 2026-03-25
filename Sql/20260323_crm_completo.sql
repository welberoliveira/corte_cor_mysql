SET NOCOUNT ON;

/*
    CRM completo para CorteCor
    Escopo:
    - Perfil CRM do cliente
    - Interações digitais
    - Tarefas de follow-up
    - Funil e oportunidades
    - Campanhas e histórico de disparo
*/

IF OBJECT_ID('dbo.CorteCor_CrmPessoaPerfil', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_CrmPessoaPerfil
    (
        IdPerfil INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdPessoa INT NOT NULL,
        StatusRelacionamento NVARCHAR(40) NOT NULL CONSTRAINT DF_CrmPessoaPerfil_StatusRelacionamento DEFAULT ('Cliente'),
        OrigemLead NVARCHAR(80) NULL,
        Temperatura NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmPessoaPerfil_Temperatura DEFAULT ('Morno'),
        ScoreRelacionamento INT NOT NULL CONSTRAINT DF_CrmPessoaPerfil_Score DEFAULT (0),
        PermiteEmail BIT NOT NULL CONSTRAINT DF_CrmPessoaPerfil_PermiteEmail DEFAULT (1),
        PermiteSms BIT NOT NULL CONSTRAINT DF_CrmPessoaPerfil_PermiteSms DEFAULT (1),
        PermiteWhatsapp BIT NOT NULL CONSTRAINT DF_CrmPessoaPerfil_PermiteWhatsapp DEFAULT (1),
        NaoPerturbe BIT NOT NULL CONSTRAINT DF_CrmPessoaPerfil_NaoPerturbe DEFAULT (0),
        UltimoContatoEm DATETIME NULL,
        ProximaAcaoEm DATETIME NULL,
        ObservacoesInternas NVARCHAR(MAX) NULL,
        DataAtualizacao DATETIME NOT NULL CONSTRAINT DF_CrmPessoaPerfil_DataAtualizacao DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID('dbo.CorteCor_CrmInteracao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_CrmInteracao
    (
        IdInteracao INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdPessoa INT NOT NULL,
        IdUsuario INT NULL,
        Canal NVARCHAR(30) NOT NULL,
        Tipo NVARCHAR(40) NOT NULL,
        Assunto NVARCHAR(160) NOT NULL,
        Descricao NVARCHAR(MAX) NULL,
        DataInteracao DATETIME NOT NULL CONSTRAINT DF_CrmInteracao_DataInteracao DEFAULT (GETDATE()),
        Referencia NVARCHAR(100) NULL,
        OrigemSistema BIT NOT NULL CONSTRAINT DF_CrmInteracao_OrigemSistema DEFAULT (0)
    );
END;

IF OBJECT_ID('dbo.CorteCor_CrmTarefa', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_CrmTarefa
    (
        IdTarefa INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdPessoa INT NULL,
        IdUsuarioResponsavel INT NULL,
        Titulo NVARCHAR(160) NOT NULL,
        Descricao NVARCHAR(MAX) NULL,
        Prioridade NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmTarefa_Prioridade DEFAULT ('Media'),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmTarefa_Status DEFAULT ('Aberta'),
        CanalSugerido NVARCHAR(20) NULL,
        DataVencimento DATETIME NOT NULL,
        DataConclusao DATETIME NULL,
        DataCriacao DATETIME NOT NULL CONSTRAINT DF_CrmTarefa_DataCriacao DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID('dbo.CorteCor_CrmEtapaFunil', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_CrmEtapaFunil
    (
        IdEtapa INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        Nome NVARCHAR(80) NOT NULL,
        Ordem INT NOT NULL,
        Ganha BIT NOT NULL CONSTRAINT DF_CrmEtapaFunil_Ganha DEFAULT (0),
        Perdida BIT NOT NULL CONSTRAINT DF_CrmEtapaFunil_Perdida DEFAULT (0),
        Ativa BIT NOT NULL CONSTRAINT DF_CrmEtapaFunil_Ativa DEFAULT (1)
    );
END;

IF OBJECT_ID('dbo.CorteCor_CrmOportunidade', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_CrmOportunidade
    (
        IdOportunidade INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdPessoa INT NOT NULL,
        IdEtapa INT NOT NULL,
        Titulo NVARCHAR(160) NOT NULL,
        Descricao NVARCHAR(MAX) NULL,
        ValorEstimado DECIMAL(18,2) NOT NULL CONSTRAINT DF_CrmOportunidade_Valor DEFAULT (0),
        Probabilidade INT NOT NULL CONSTRAINT DF_CrmOportunidade_Probabilidade DEFAULT (0),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmOportunidade_Status DEFAULT ('Aberta'),
        Origem NVARCHAR(80) NULL,
        PrevisaoFechamento DATE NULL,
        DataCriacao DATETIME NOT NULL CONSTRAINT DF_CrmOportunidade_DataCriacao DEFAULT (GETDATE()),
        DataAtualizacao DATETIME NOT NULL CONSTRAINT DF_CrmOportunidade_DataAtualizacao DEFAULT (GETDATE()),
        DataFechamento DATETIME NULL
    );
END;

IF OBJECT_ID('dbo.CorteCor_CrmCampanha', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_CrmCampanha
    (
        IdCampanha INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdSalao INT NOT NULL,
        Nome NVARCHAR(120) NOT NULL,
        Canal NVARCHAR(20) NOT NULL,
        Segmento NVARCHAR(40) NOT NULL,
        FiltroTag NVARCHAR(120) NULL,
        DiasInatividade INT NULL,
        IdPessoa INT NULL,
        Assunto NVARCHAR(160) NULL,
        Conteudo NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_CrmCampanha_Status DEFAULT ('Rascunho'),
        TotalDestinatarios INT NOT NULL CONSTRAINT DF_CrmCampanha_TotalDest DEFAULT (0),
        TotalSucesso INT NOT NULL CONSTRAINT DF_CrmCampanha_TotalSucesso DEFAULT (0),
        TotalFalha INT NOT NULL CONSTRAINT DF_CrmCampanha_TotalFalha DEFAULT (0),
        UltimoEnvioEm DATETIME NULL,
        DataCriacao DATETIME NOT NULL CONSTRAINT DF_CrmCampanha_DataCriacao DEFAULT (GETDATE())
    );
END;

IF OBJECT_ID('dbo.CorteCor_CrmCampanhaDestino', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_CrmCampanhaDestino
    (
        IdDestino INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdCampanha INT NOT NULL,
        IdSalao INT NOT NULL,
        IdPessoa INT NOT NULL,
        Canal NVARCHAR(20) NOT NULL,
        Destino NVARCHAR(180) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        MensagemErro NVARCHAR(MAX) NULL,
        DataEnvio DATETIME NOT NULL CONSTRAINT DF_CrmCampanhaDestino_DataEnvio DEFAULT (GETDATE())
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CorteCor_CrmPessoaPerfil_Salao_Pessoa' AND object_id = OBJECT_ID('dbo.CorteCor_CrmPessoaPerfil'))
BEGIN
    CREATE UNIQUE INDEX UX_CorteCor_CrmPessoaPerfil_Salao_Pessoa
        ON dbo.CorteCor_CrmPessoaPerfil (IdSalao, IdPessoa);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmInteracao_Salao_Pessoa_Data' AND object_id = OBJECT_ID('dbo.CorteCor_CrmInteracao'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmInteracao_Salao_Pessoa_Data
        ON dbo.CorteCor_CrmInteracao (IdSalao, IdPessoa, DataInteracao DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmTarefa_Salao_Status_Vencimento' AND object_id = OBJECT_ID('dbo.CorteCor_CrmTarefa'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmTarefa_Salao_Status_Vencimento
        ON dbo.CorteCor_CrmTarefa (IdSalao, Status, DataVencimento);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmTarefa_Salao_Pessoa' AND object_id = OBJECT_ID('dbo.CorteCor_CrmTarefa'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmTarefa_Salao_Pessoa
        ON dbo.CorteCor_CrmTarefa (IdSalao, IdPessoa);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmEtapaFunil_Salao_Ordem' AND object_id = OBJECT_ID('dbo.CorteCor_CrmEtapaFunil'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmEtapaFunil_Salao_Ordem
        ON dbo.CorteCor_CrmEtapaFunil (IdSalao, Ordem);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmOportunidade_Salao_Status' AND object_id = OBJECT_ID('dbo.CorteCor_CrmOportunidade'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmOportunidade_Salao_Status
        ON dbo.CorteCor_CrmOportunidade (IdSalao, Status, IdEtapa);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmOportunidade_Salao_Pessoa' AND object_id = OBJECT_ID('dbo.CorteCor_CrmOportunidade'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmOportunidade_Salao_Pessoa
        ON dbo.CorteCor_CrmOportunidade (IdSalao, IdPessoa);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmCampanha_Salao_DataCriacao' AND object_id = OBJECT_ID('dbo.CorteCor_CrmCampanha'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmCampanha_Salao_DataCriacao
        ON dbo.CorteCor_CrmCampanha (IdSalao, DataCriacao DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmCampanhaDestino_Salao_Campanha_DataEnvio' AND object_id = OBJECT_ID('dbo.CorteCor_CrmCampanhaDestino'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmCampanhaDestino_Salao_Campanha_DataEnvio
        ON dbo.CorteCor_CrmCampanhaDestino (IdSalao, IdCampanha, DataEnvio DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_CrmCampanhaDestino_Salao_Pessoa' AND object_id = OBJECT_ID('dbo.CorteCor_CrmCampanhaDestino'))
BEGIN
    CREATE INDEX IX_CorteCor_CrmCampanhaDestino_Salao_Pessoa
        ON dbo.CorteCor_CrmCampanhaDestino (IdSalao, IdPessoa);
END;
