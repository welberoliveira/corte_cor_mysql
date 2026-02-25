-- TABELA: Armazenamento de Eventos (Cancelamentos e Cartas de Correção)
CREATE TABLE CorteCor_NotaFiscalEvento (
    IdEvento UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    IdNotaFiscal UNIQUEIDENTIFIER NOT NULL,
    IdSalao INT NOT NULL,
    TipoEvento VARCHAR(50) NOT NULL, -- 'Cancelamento', 'Carta de Correção'
    Justificativa VARCHAR(255) NOT NULL,
    ProtocoloEvento VARCHAR(50) NULL,
    XmlEnvio NVARCHAR(MAX) NULL,
    XmlRetorno NVARCHAR(MAX) NULL,
    Status VARCHAR(20) NOT NULL, -- 'Autorizado', 'Rejeitado'
    DataRegistro DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Evento_NotaFiscal FOREIGN KEY (IdNotaFiscal) REFERENCES CorteCor_NotaFiscal(IdNotaFiscal)
);
GO

-- TABELA: Inutilização de Faixa de Numerações (Exclusivo NFC-e/NF-e)
CREATE TABLE CorteCor_NotaFiscalInutilizacao (
    IdInutilizacao UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    IdSalao INT NOT NULL,
    Ano INT NOT NULL,
    Modelo INT NOT NULL, -- 65 = NFC-e, 55 = NF-e
    Serie INT NOT NULL,
    NumeroInicial INT NOT NULL,
    NumeroFinal INT NOT NULL,
    Justificativa VARCHAR(255) NOT NULL,
    Status VARCHAR(20) NOT NULL, -- 'Homologado', 'Rejeitado'
    Protocolo VARCHAR(50) NULL,
    XmlRetorno NVARCHAR(MAX) NULL,
    DataInutilizacao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Inutilizacao_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);
GO
