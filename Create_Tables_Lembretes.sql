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
