/* =========================================================
   CORTE & COR – SCRIPT COMPLETO DE CRIAÇÃO DAS TABELAS
   ========================================================= */

------------------------------------------------------------
-- 1) SALÃO
------------------------------------------------------------
CREATE TABLE [dbo].[CorteCor_Salao](
    [IdSalao] INT IDENTITY(1,1) NOT NULL,
    [Nome] NVARCHAR(255) NOT NULL,
    [Responsavel] NVARCHAR(255) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [Telefone] NVARCHAR(20) NULL,
    [Endereco] NVARCHAR(255) NULL,
    [CNPJ] NVARCHAR(20) NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Ativo',
    [DataCadastro] DATETIME NOT NULL DEFAULT GETDATE(),
    [Observacao] NVARCHAR(MAX) NULL,
    CONSTRAINT PK_CorteCor_Salao PRIMARY KEY (IdSalao),
    CONSTRAINT UQ_CorteCor_Salao_Email UNIQUE (Email),
    CONSTRAINT UQ_CorteCor_Salao_CNPJ UNIQUE (CNPJ)
);
GO

------------------------------------------------------------
-- 2) ADMINISTRADOR
------------------------------------------------------------
CREATE TABLE [dbo].[CorteCor_Administrador](
    [IdUsuario] INT IDENTITY(1,1) NOT NULL,
    [Nome] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [Senha] NVARCHAR(255) NOT NULL,
    [Perfil] NVARCHAR(50) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Ativo',
    [DataCriacao] DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_CorteCor_Administrador PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_CorteCor_Administrador_Email UNIQUE (Email)
);
GO

------------------------------------------------------------
-- 3) USUÁRIO (FUNCIONAL / SISTEMA)
------------------------------------------------------------
CREATE TABLE [dbo].[CorteCor_Usuario](
    [IdUsuario] INT IDENTITY(1,1) NOT NULL,
    [Nome] NVARCHAR(100) NOT NULL,
    [Sobrenome] NVARCHAR(100) NOT NULL DEFAULT '',
    [Email] NVARCHAR(100) NULL,
    [Telefone] NVARCHAR(15) NULL,
    [CPF] NVARCHAR(14) NOT NULL DEFAULT '000.000.000-00',
    [Senha] NVARCHAR(255) NOT NULL DEFAULT 'Senha123',
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Ativo',
    [DataEntrada] DATE NOT NULL,
    [DataSaida] DATE NULL,
    [IdSalao] INT NOT NULL,
    CONSTRAINT PK_CorteCor_Usuario PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_CorteCor_Usuario_Email_Salao UNIQUE (Email, IdSalao),
    CONSTRAINT FK_CorteCor_Usuario_Salao
        FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);
GO

------------------------------------------------------------
-- 4) FUNCIONÁRIO
------------------------------------------------------------
CREATE TABLE [dbo].[CorteCor_Funcionario](
    [IdFuncionario] INT IDENTITY(1,1) NOT NULL,
    [Nome] NVARCHAR(150) NOT NULL,

    [seg] BIT NOT NULL DEFAULT 0,
    [seg_ini] TIME NULL,
    [seg_fim] TIME NULL,

    [ter] BIT NOT NULL DEFAULT 0,
    [ter_ini] TIME NULL,
    [ter_fim] TIME NULL,

    [qua] BIT NOT NULL DEFAULT 0,
    [qua_ini] TIME NULL,
    [qua_fim] TIME NULL,

    [qui] BIT NOT NULL DEFAULT 0,
    [qui_ini] TIME NULL,
    [qui_fim] TIME NULL,

    [sex] BIT NOT NULL DEFAULT 0,
    [sex_ini] TIME NULL,
    [sex_fim] TIME NULL,

    [sab] BIT NOT NULL DEFAULT 0,
    [sab_ini] TIME NULL,
    [sab_fim] TIME NULL,

    [dom] BIT NOT NULL DEFAULT 0,
    [dom_ini] TIME NULL,
    [dom_fim] TIME NULL,

    [IdSalao] INT NOT NULL,

    CONSTRAINT PK_CorteCor_Funcionario PRIMARY KEY (IdFuncionario),
    CONSTRAINT FK_CorteCor_Funcionario_Salao
        FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);
GO

------------------------------------------------------------
-- 5) SERVIÇO
------------------------------------------------------------
CREATE TABLE [dbo].[CorteCor_Servico](
    [IdServico] INT IDENTITY(1,1) NOT NULL,
    [Nome] NVARCHAR(150) NOT NULL,
    [Preco] DECIMAL(10,2) NOT NULL,
    [IdSalao] INT NOT NULL,
    [Duracao] TIME(0) NOT NULL,
    CONSTRAINT PK_CorteCor_Servico PRIMARY KEY (IdServico),
    CONSTRAINT FK_CorteCor_Servico_Salao
        FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);
GO

------------------------------------------------------------
-- 6) FUNCIONÁRIO x SERVIÇO (N:N)
------------------------------------------------------------
CREATE TABLE [dbo].[CorteCor_Funcionario_Servico](
    [IdFuncionario] INT NOT NULL,
    [IdServico] INT NOT NULL,
    CONSTRAINT PK_CorteCor_Funcionario_Servico
        PRIMARY KEY (IdFuncionario, IdServico),
    CONSTRAINT FK_FS_Funcionario
        FOREIGN KEY (IdFuncionario)
        REFERENCES CorteCor_Funcionario(IdFuncionario),
    CONSTRAINT FK_FS_Servico
        FOREIGN KEY (IdServico)
        REFERENCES CorteCor_Servico(IdServico)
);
GO

------------------------------------------------------------
-- 7) PESSOA (CLIENTE)
------------------------------------------------------------
CREATE TABLE [dbo].[CorteCor_Pessoa](
    [IdPessoa] INT IDENTITY(1,1) NOT NULL,
    [Nome] NVARCHAR(150) NOT NULL,
    [Telefone] NVARCHAR(20) NULL,
    [Email] NVARCHAR(150) NULL,
    [DataNascimento] DATE NULL,
    [IdSalao] INT NOT NULL,
    CONSTRAINT PK_CorteCor_Pessoa PRIMARY KEY (IdPessoa),
    CONSTRAINT FK_CorteCor_Pessoa_Salao
        FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);
GO


CREATE TABLE [dbo].[CorteCor_Agendamento](
    [IdAgendamento] INT IDENTITY(1,1) NOT NULL,
    [DataHora] DATETIME2(0) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Agendado',

    [IdServico] INT NOT NULL,
    [IdPessoa] INT NOT NULL,
    [IdFuncionario] INT NOT NULL,

    CONSTRAINT [PK_CorteCor_Agendamento] PRIMARY KEY ([IdAgendamento]),

    CONSTRAINT [FK_CorteCor_Agendamento_Servico]
        FOREIGN KEY ([IdServico]) REFERENCES [dbo].[CorteCor_Servico]([IdServico]),

    CONSTRAINT [FK_CorteCor_Agendamento_Pessoa]
        FOREIGN KEY ([IdPessoa]) REFERENCES [dbo].[CorteCor_Pessoa]([IdPessoa]),

    CONSTRAINT [FK_CorteCor_Agendamento_Funcionario]
        FOREIGN KEY ([IdFuncionario]) REFERENCES [dbo].[CorteCor_Funcionario]([IdFuncionario])
);
GO

-- (Opcional, recomendado) índice para buscas por agenda do funcionário
CREATE INDEX [IX_CorteCor_Agendamento_Funcionario_DataHora]
ON [dbo].[CorteCor_Agendamento] ([IdFuncionario], [DataHora]);
GO


CREATE TABLE [dbo].[CorteCor_MeioPagamento](
    [IdMeioPagamento] INT IDENTITY(1,1) NOT NULL,

    [Nome] NVARCHAR(80) NOT NULL,       
    -- Ex: Pix Mercado Pago, Cartão Crédito Mercado Pago

    [Tipo] NVARCHAR(30) NOT NULL,       
    -- Ex: PIX, CREDITO, DEBITO, BOLETO

    [Gateway] NVARCHAR(50) NOT NULL,    
    -- Ex: MercadoPago, PagSeguro, Stripe, Pagarme

    [PermiteParcelamento] BIT NOT NULL DEFAULT 0,
    [ParcelasMax] TINYINT NULL,         

    [TaxaPercentual] DECIMAL(6,3) NOT NULL DEFAULT 0,
    [TaxaFixa] DECIMAL(10,2) NOT NULL DEFAULT 0,

    [PrazoRecebimentoDias] SMALLINT NOT NULL DEFAULT 0,
    -- Ex: PIX=0 | Crédito=14, 30

    [Ativo] BIT NOT NULL DEFAULT 1,

    [IdSalao] INT NOT NULL,
    [DataCadastro] DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_CorteCor_MeioPagamento 
        PRIMARY KEY (IdMeioPagamento),

    CONSTRAINT FK_CorteCor_MeioPagamento_Salao
        FOREIGN KEY (IdSalao)
        REFERENCES CorteCor_Salao(IdSalao),

    CONSTRAINT UQ_CorteCor_MeioPagamento_Salao_Nome
        UNIQUE (IdSalao, Nome)
);
GO


CREATE TABLE [dbo].[CorteCor_Pagamento](
    [IdPagamento] INT IDENTITY(1,1) NOT NULL,

    [Contos] NVARCHAR(255) NULL,            -- (texto livre) observação/descrição do pagamento
    [Campos] NVARCHAR(MAX) NULL,            -- (texto) dados extras (ex: JSON do gateway, metadados)
    [Data] DATETIME2(0) NOT NULL DEFAULT GETDATE(),
    [Valor] DECIMAL(10,2) NOT NULL,
    [Tipo] NVARCHAR(30) NOT NULL,           -- Ex: PIX, CREDITO, DEBITO, BOLETO (ou conforme sua regra)

    [IdMeioPagamento] INT NOT NULL,
    [IdAgendamento] INT NOT NULL,

    CONSTRAINT [PK_CorteCor_Pagamento] PRIMARY KEY ([IdPagamento]),

    CONSTRAINT [FK_CorteCor_Pagamento_MeioPagamento]
        FOREIGN KEY ([IdMeioPagamento])
        REFERENCES [dbo].[CorteCor_MeioPagamento]([IdMeioPagamento]),

    CONSTRAINT [FK_CorteCor_Pagamento_Agendamento]
        FOREIGN KEY ([IdAgendamento])
        REFERENCES [dbo].[CorteCor_Agendamento]([IdAgendamento])
);
GO

-- Índices recomendados
CREATE INDEX [IX_CorteCor_Pagamento_Agendamento_Data]
ON [dbo].[CorteCor_Pagamento] ([IdAgendamento], [Data]);
GO

CREATE INDEX [IX_CorteCor_Pagamento_MeioPagamento_Data]
ON [dbo].[CorteCor_Pagamento] ([IdMeioPagamento], [Data]);
GO


-- 1) Tabela de pagamentos vinculados a agendamentos
CREATE TABLE dbo.CorteCor_Pagamento (
    IdPagamento               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CorteCor_Pagamento_IdPagamento DEFAULT NEWID(),
    IdAgendamento             INT NOT NULL,

    Ativo                     BIT NOT NULL CONSTRAINT DF_CorteCor_Pagamento_Ativo DEFAULT (1),
    Status                    VARCHAR(20) NOT NULL CONSTRAINT DF_CorteCor_Pagamento_Status DEFAULT ('Pendente'), 
    Valor                     DECIMAL(18,2) NOT NULL,
    Moeda                     CHAR(3) NOT NULL CONSTRAINT DF_CorteCor_Pagamento_Moeda DEFAULT ('BRL'),
    Descricao                 NVARCHAR(200) NULL,

    MercadoPagoPreferenceId   NVARCHAR(80) NULL,
    MercadoPagoPaymentId      NVARCHAR(30) NULL,
    CheckoutUrl               NVARCHAR(500) NULL,

    MpStatus                  NVARCHAR(30) NULL,
    MpStatusDetail            NVARCHAR(80) NULL,

    CriadoEm                  DATETIME2(0) NOT NULL CONSTRAINT DF_CorteCor_Pagamento_CriadoEm DEFAULT SYSUTCDATETIME(),
    AtualizadoEm              DATETIME2(0) NULL,
    PagoEm                    DATETIME2(0) NULL,

    CONSTRAINT PK_CorteCor_Pagamento PRIMARY KEY (IdPagamento)
);

-- 2) FK para o agendamento (ajuste o nome da PK/tabela se necessário)
ALTER TABLE dbo.CorteCor_Pagamento
ADD CONSTRAINT FK_CorteCor_Pagamento_Agendamento
FOREIGN KEY (IdAgendamento) REFERENCES dbo.CorteCor_Agendamento(IdAgendamento);

-- 3) Um agendamento pode ter só 1 pagamento "ativo" por vez (permite reemitir depois marcando Ativo=0)
CREATE UNIQUE INDEX UX_CorteCor_Pagamento_Agendamento_Ativo
ON dbo.CorteCor_Pagamento (IdAgendamento)
WHERE Ativo = 1;

-- 4) Índice para localizar rápido por IDs vindos do Mercado Pago
CREATE INDEX IX_CorteCor_Pagamento_MercadoPagoPaymentId
ON dbo.CorteCor_Pagamento (MercadoPagoPaymentId);


CREATE TABLE [dbo].[CorteCor_ModeloEmail](
    [IdModelo] INT IDENTITY(1,1) NOT NULL,
    [IdSalao] INT NOT NULL,
    [TipoEvento] NVARCHAR(50) NOT NULL, -- Ex: 'BoasVindas', 'Confirmacao', 'Cancelamento', 'LembretePagamento'
    [Assunto] NVARCHAR(255) NOT NULL,
    [CorpoHTML] NVARCHAR(MAX) NOT NULL,
    [Ativo] BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_CorteCor_ModeloEmail PRIMARY KEY ([IdModelo]),
    CONSTRAINT FK_CorteCor_ModeloEmail_Salao FOREIGN KEY ([IdSalao]) REFERENCES [dbo].[CorteCor_Salao]([IdSalao]),
    CONSTRAINT UQ_ModeloEmail_Salao_Evento UNIQUE ([IdSalao], [TipoEvento]) -- Apenas 1 modelo ativo por evento por salão
);
GO

-- Tabela para configurar regras de lembretes por salão
CREATE TABLE CorteCor_LembreteConfig (
    IdConfig INT IDENTITY(1,1) PRIMARY KEY,
    IdSalao INT NOT NULL,
    AntecedenciaValor INT NOT NULL, -- Ex: 2, 1
    AntecedenciaUnidade VARCHAR(10) NOT NULL, -- 'Horas', 'Dias'
    IdModeloEmail INT NULL, -- Opcional: qual template usar
    Ativo BIT NOT NULL DEFAULT 1,
    DataCriacao DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_LembreteConfig_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
    -- CONSTRAINT FK_LembreteConfig_ModeloEmail FOREIGN KEY (IdModeloEmail) REFERENCES CorteCor_ModeloEmail(IdModelo) -- Uncomment if ModeloEmail table exists and you want FK
);

-- Tabela para armazenar os lembretes agendados para envio
CREATE TABLE CorteCor_LembreteAgendado (
    IdLembrete INT IDENTITY(1,1) PRIMARY KEY,
    IdAgendamento INT NOT NULL,
    IdConfig INT NOT NULL, -- Qual regra gerou este lembrete
    DataEnvioProgramada DATETIME NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pendente', -- 'Pendente', 'Enviado', 'Erro', 'Cancelado'
    Tentativas INT DEFAULT 0,
    UltimoErro VARCHAR(MAX) NULL,
    DataEnvioReal DATETIME NULL,
    DataCriacao DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_LembreteAgendado_Agendamento FOREIGN KEY (IdAgendamento) REFERENCES CorteCor_Agendamento(IdAgendamento),
    CONSTRAINT FK_LembreteAgendado_Config FOREIGN KEY (IdConfig) REFERENCES CorteCor_LembreteConfig(IdConfig)
);

-- Índices para performance na busca de lembretes pendentes
CREATE INDEX IX_LembreteAgendado_Status_DataEnvio ON CorteCor_LembreteAgendado(Status, DataEnvioProgramada);


CREATE TABLE CorteCor_LogEnvioEmail (
    IdLog INT IDENTITY(1,1) PRIMARY KEY,
    IdLembrete INT NOT NULL,
    IdAgendamento INT NOT NULL,
    DataEnvio DATETIME NOT NULL,
    Destinatario VARCHAR(200) NOT NULL,
    Assunto VARCHAR(200) NOT NULL,
    Status VARCHAR(50) NOT NULL, -- 'Sucesso' ou 'ErroEnvio'
    MensagemErro VARCHAR(MAX) NULL,
    INDEX IX_LogEnvioEmail_DataEnvio (DataEnvio)
);