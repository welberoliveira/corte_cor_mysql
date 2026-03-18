SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.CorteCor_SalaoConfigFiscal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_SalaoConfigFiscal
    (
        IdConfigFiscal UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CorteCor_SalaoConfigFiscal PRIMARY KEY DEFAULT NEWID(),
        IdSalao INT NOT NULL,
        Cnpj VARCHAR(14) NULL,
        RazaoSocial VARCHAR(150) NULL,
        InscricaoEstadual VARCHAR(20) NULL,
        InscricaoMunicipal VARCHAR(20) NULL,
        Ambiente INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_Ambiente DEFAULT 2,
        EmissaoAutomatica BIT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_EmissaoAutomatica DEFAULT 0,
        CodigoMunicipioIBGE INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_CodigoMunicipio DEFAULT 0,
        CodigoUFIBGE INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_CodigoUF DEFAULT 0,
        RegimeTributario INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_Regime DEFAULT 1,
        CertificadoPfx VARBINARY(MAX) NULL,
        CertificadoSenha VARBINARY(MAX) NULL,
        CertificadoValidade DATETIME NULL,
        TokenNfse NVARCHAR(100) NULL,
        CSC NVARCHAR(50) NULL,
        IdCSC NVARCHAR(20) NULL,
        SerieNFCe INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_SerieNFCe DEFAULT 1,
        NumeroNFCe INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_NumeroNFCe DEFAULT 1,
        SerieNFSe INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_SerieNFSe DEFAULT 1,
        NumeroNFSe INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_NumeroNFSe DEFAULT 1,
        RegimeEspecialTributacao INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_RegimeEspecial DEFAULT 0,
        IssExigibilidade INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_IssExigibilidade DEFAULT 1,
        IssRetido INT NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_IssRetido DEFAULT 2,
        EnderecoLogradouro NVARCHAR(150) NULL,
        EnderecoNumero NVARCHAR(20) NULL,
        EnderecoBairro NVARCHAR(100) NULL,
        EnderecoCep NVARCHAR(10) NULL,
        EnderecoCidade NVARCHAR(100) NULL,
        EnderecoUF NVARCHAR(2) NULL,
        Telefone NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        DataAtualizacao DATETIME NOT NULL CONSTRAINT DF_CorteCor_SalaoConfigFiscal_DataAtualizacao DEFAULT GETDATE()
    );
END
GO

IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'EmissaoAutomatica') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD EmissaoAutomatica BIT NOT NULL CONSTRAINT DF_CCSCF_EmissaoAutomatica DEFAULT 0;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'TokenNfse') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD TokenNfse NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'CSC') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD CSC NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'IdCSC') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD IdCSC NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'SerieNFCe') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD SerieNFCe INT NOT NULL CONSTRAINT DF_CCSCF_SerieNFCe DEFAULT 1;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'NumeroNFCe') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD NumeroNFCe INT NOT NULL CONSTRAINT DF_CCSCF_NumeroNFCe DEFAULT 1;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'SerieNFSe') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD SerieNFSe INT NOT NULL CONSTRAINT DF_CCSCF_SerieNFSe DEFAULT 1;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'NumeroNFSe') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD NumeroNFSe INT NOT NULL CONSTRAINT DF_CCSCF_NumeroNFSe DEFAULT 1;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'RegimeEspecialTributacao') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD RegimeEspecialTributacao INT NOT NULL CONSTRAINT DF_CCSCF_RegimeEspecial DEFAULT 0;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'IssExigibilidade') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD IssExigibilidade INT NOT NULL CONSTRAINT DF_CCSCF_IssExigibilidade DEFAULT 1;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'IssRetido') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD IssRetido INT NOT NULL CONSTRAINT DF_CCSCF_IssRetido DEFAULT 2;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'EnderecoLogradouro') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD EnderecoLogradouro NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'EnderecoNumero') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD EnderecoNumero NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'EnderecoBairro') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD EnderecoBairro NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'EnderecoCep') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD EnderecoCep NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'EnderecoCidade') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD EnderecoCidade NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'EnderecoUF') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD EnderecoUF NVARCHAR(2) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'Telefone') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD Telefone NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.CorteCor_SalaoConfigFiscal', 'Email') IS NULL
    ALTER TABLE dbo.CorteCor_SalaoConfigFiscal ADD Email NVARCHAR(100) NULL;
GO

IF OBJECT_ID(N'dbo.CorteCor_NotaFiscal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_NotaFiscal
    (
        IdNotaFiscal UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CorteCor_NotaFiscal PRIMARY KEY DEFAULT NEWID(),
        IdSalao INT NOT NULL,
        IdAgendamento INT NULL,
        IdVendaProduto INT NULL,
        TipoNota VARCHAR(10) NOT NULL,
        Ambiente INT NOT NULL,
        Numero INT NOT NULL,
        Serie INT NOT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Status VARCHAR(30) NOT NULL CONSTRAINT DF_CorteCor_NotaFiscal_Status DEFAULT 'Pendente',
        ChaveAcesso VARCHAR(80) NULL,
        ChaveAcessoNacional VARCHAR(80) NULL,
        NumeroNFSeNacional VARCHAR(50) NULL,
        NumeroRecibo VARCHAR(50) NULL,
        ProtocoloAutorizacao VARCHAR(255) NULL,
        JustificativaRejeicao VARCHAR(1000) NULL,
        XmlEnvio NVARCHAR(MAX) NULL,
        XmlRetorno NVARCHAR(MAX) NULL,
        DataEmissao DATETIME NOT NULL CONSTRAINT DF_CorteCor_NotaFiscal_DataEmissao DEFAULT GETDATE(),
        DataAtualizacao DATETIME NOT NULL CONSTRAINT DF_CorteCor_NotaFiscal_DataAtualizacao DEFAULT GETDATE()
    );
END
GO

IF COL_LENGTH('dbo.CorteCor_NotaFiscal', 'ChaveAcessoNacional') IS NULL
    ALTER TABLE dbo.CorteCor_NotaFiscal ADD ChaveAcessoNacional VARCHAR(80) NULL;
IF COL_LENGTH('dbo.CorteCor_NotaFiscal', 'NumeroNFSeNacional') IS NULL
    ALTER TABLE dbo.CorteCor_NotaFiscal ADD NumeroNFSeNacional VARCHAR(50) NULL;
IF COL_LENGTH('dbo.CorteCor_NotaFiscal', 'NumeroRecibo') IS NULL
    ALTER TABLE dbo.CorteCor_NotaFiscal ADD NumeroRecibo VARCHAR(50) NULL;
GO

IF OBJECT_ID(N'dbo.CorteCor_NotaFiscalEvento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_NotaFiscalEvento
    (
        IdEvento UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CorteCor_NotaFiscalEvento PRIMARY KEY DEFAULT NEWID(),
        IdNotaFiscal UNIQUEIDENTIFIER NOT NULL,
        IdSalao INT NOT NULL,
        TipoEvento VARCHAR(50) NOT NULL,
        Justificativa VARCHAR(500) NOT NULL,
        ProtocoloEvento VARCHAR(255) NULL,
        XmlEnvio NVARCHAR(MAX) NULL,
        XmlRetorno NVARCHAR(MAX) NULL,
        Status VARCHAR(100) NOT NULL,
        DataRegistro DATETIME NOT NULL CONSTRAINT DF_CorteCor_NotaFiscalEvento_DataRegistro DEFAULT GETDATE()
    );
END
GO

IF OBJECT_ID(N'dbo.CorteCor_NotaFiscalInutilizacao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_NotaFiscalInutilizacao
    (
        IdInutilizacao UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CorteCor_NotaFiscalInutilizacao PRIMARY KEY DEFAULT NEWID(),
        IdSalao INT NOT NULL,
        Ano INT NOT NULL,
        Modelo INT NOT NULL,
        Serie INT NOT NULL,
        NumeroInicial INT NOT NULL,
        NumeroFinal INT NOT NULL,
        TipoNota VARCHAR(10) NULL,
        Justificativa VARCHAR(500) NOT NULL,
        Protocolo VARCHAR(255) NULL,
        XmlEnvio NVARCHAR(MAX) NULL,
        XmlRetorno NVARCHAR(MAX) NULL,
        Status VARCHAR(100) NOT NULL,
        DataInutilizacao DATETIME NOT NULL CONSTRAINT DF_CorteCor_NotaFiscalInutilizacao_Data DEFAULT GETDATE()
    );
END
GO

IF OBJECT_ID(N'dbo.CorteCor_NotaFiscalLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorteCor_NotaFiscalLog
    (
        IdLog INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CorteCor_NotaFiscalLog PRIMARY KEY,
        IdSalao INT NOT NULL,
        IdNotaFiscal UNIQUEIDENTIFIER NULL,
        IdAgendamento INT NULL,
        DataHora DATETIME NOT NULL CONSTRAINT DF_CorteCor_NotaFiscalLog_DataHora DEFAULT GETDATE(),
        TipoEvento NVARCHAR(100) NULL,
        RequestPayload NVARCHAR(MAX) NULL,
        ResponsePayload NVARCHAR(MAX) NULL,
        CodigoErro NVARCHAR(100) NULL,
        Mensagem NVARCHAR(MAX) NULL,
        Usuario NVARCHAR(150) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.CorteCor_NotaFiscalLog', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.CorteCor_NotaFiscalLog', 'TipoEvento') IS NULL
        ALTER TABLE dbo.CorteCor_NotaFiscalLog ADD TipoEvento NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.CorteCor_NotaFiscalLog', 'RequestPayload') IS NULL
        ALTER TABLE dbo.CorteCor_NotaFiscalLog ADD RequestPayload NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.CorteCor_NotaFiscalLog', 'ResponsePayload') IS NULL
        ALTER TABLE dbo.CorteCor_NotaFiscalLog ADD ResponsePayload NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.CorteCor_NotaFiscalLog', 'CodigoErro') IS NULL
        ALTER TABLE dbo.CorteCor_NotaFiscalLog ADD CodigoErro NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.CorteCor_NotaFiscalLog', 'Mensagem') IS NULL
        ALTER TABLE dbo.CorteCor_NotaFiscalLog ADD Mensagem NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.CorteCor_NotaFiscalLog', 'Usuario') IS NULL
        ALTER TABLE dbo.CorteCor_NotaFiscalLog ADD Usuario NVARCHAR(150) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscal_Salao_Data' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal'))
    CREATE INDEX IX_CorteCor_NotaFiscal_Salao_Data ON dbo.CorteCor_NotaFiscal(IdSalao, DataEmissao DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscal_Status_Tipo' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal'))
    CREATE INDEX IX_CorteCor_NotaFiscal_Status_Tipo ON dbo.CorteCor_NotaFiscal(Status, TipoNota, IdSalao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscal_ChaveAcesso' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal'))
    CREATE INDEX IX_CorteCor_NotaFiscal_ChaveAcesso ON dbo.CorteCor_NotaFiscal(ChaveAcesso);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscal_ChaveAcessoNacional' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal'))
    CREATE INDEX IX_CorteCor_NotaFiscal_ChaveAcessoNacional ON dbo.CorteCor_NotaFiscal(ChaveAcessoNacional);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscal_Serie_Numero' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal'))
    CREATE INDEX IX_CorteCor_NotaFiscal_Serie_Numero ON dbo.CorteCor_NotaFiscal(IdSalao, TipoNota, Ambiente, Serie, Numero);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscalEvento_Nota' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscalEvento'))
    CREATE INDEX IX_CorteCor_NotaFiscalEvento_Nota ON dbo.CorteCor_NotaFiscalEvento(IdNotaFiscal, DataRegistro DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscalInutilizacao_Salao_Data' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscalInutilizacao'))
    CREATE INDEX IX_CorteCor_NotaFiscalInutilizacao_Salao_Data ON dbo.CorteCor_NotaFiscalInutilizacao(IdSalao, DataInutilizacao DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscalLog_Salao_Data' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscalLog'))
    CREATE INDEX IX_CorteCor_NotaFiscalLog_Salao_Data ON dbo.CorteCor_NotaFiscalLog(IdSalao, DataHora DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CorteCor_NotaFiscalLog_Nota' AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscalLog'))
    CREATE INDEX IX_CorteCor_NotaFiscalLog_Nota ON dbo.CorteCor_NotaFiscalLog(IdNotaFiscal, DataHora DESC);
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_CorteCor_SalaoConfigFiscal_IdSalao'
      AND object_id = OBJECT_ID('dbo.CorteCor_SalaoConfigFiscal')
)
BEGIN
    IF NOT EXISTS (
        SELECT IdSalao
        FROM dbo.CorteCor_SalaoConfigFiscal
        GROUP BY IdSalao
        HAVING COUNT(*) > 1
    )
    BEGIN
        CREATE UNIQUE INDEX UX_CorteCor_SalaoConfigFiscal_IdSalao ON dbo.CorteCor_SalaoConfigFiscal(IdSalao);
    END
    ELSE
    BEGIN
        RAISERROR('Nao foi possivel criar UX_CorteCor_SalaoConfigFiscal_IdSalao porque existem duplicidades em IdSalao.', 10, 1);
    END
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero'
      AND object_id = OBJECT_ID('dbo.CorteCor_NotaFiscal')
)
BEGIN
    IF NOT EXISTS (
        SELECT IdSalao, TipoNota, Ambiente, Serie, Numero
        FROM dbo.CorteCor_NotaFiscal
        GROUP BY IdSalao, TipoNota, Ambiente, Serie, Numero
        HAVING COUNT(*) > 1
    )
    BEGIN
        CREATE UNIQUE INDEX UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero
            ON dbo.CorteCor_NotaFiscal(IdSalao, TipoNota, Ambiente, Serie, Numero);
    END
    ELSE
    BEGIN
        RAISERROR('Nao foi possivel criar UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero porque existem numeracoes duplicadas.', 10, 1);
    END
END
GO
