CREATE TABLE CorteCor_ModeloEmail (
    IdModelo INT IDENTITY(1,1) PRIMARY KEY,
    IdSalao INT NOT NULL,
    TipoEvento VARCHAR(50) NOT NULL, -- 'BoasVindas', 'ConfirmacaoAgendamento', etc.
    Assunto VARCHAR(200) NOT NULL,
    CorpoHTML NVARCHAR(MAX) NOT NULL,
    Ativo BIT NOT NULL DEFAULT 1,
    DataAtualizacao DATETIME DEFAULT GETDATE()
);

-- Index for faster lookup by Salon and Event Type
CREATE INDEX IX_ModeloEmail_Salao_Evento ON CorteCor_ModeloEmail (IdSalao, TipoEvento);
