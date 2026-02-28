CREATE TABLE CorteCor_NotaFiscalLog
(
    IdLog UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    IdNotaFiscal UNIQUEIDENTIFIER NULL,
    IdAgendamento INT NULL,
    IdSalao INT NOT NULL,
    DataHora DATETIME NOT NULL,
    Etapa NVARCHAR(100) NOT NULL,
    MensagemStatus NVARCHAR(MAX) NOT NULL,
    ConteudoXml NVARCHAR(MAX) NULL
);
GO

-- Índices para facilitar a busca do relatório transacional na tela
CREATE INDEX IX_CorteCor_NotaFiscalLog_IdNotaFiscal ON CorteCor_NotaFiscalLog(IdNotaFiscal);
CREATE INDEX IX_CorteCor_NotaFiscalLog_IdAgendamento ON CorteCor_NotaFiscalLog(IdAgendamento);
CREATE INDEX IX_CorteCor_NotaFiscalLog_IdSalao ON CorteCor_NotaFiscalLog(IdSalao);
GO
