-- TABELA: Configurações Fiscais e Armazenamento do Certificado
CREATE TABLE CorteCor_SalaoConfigFiscal (
    IdConfigFiscal UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    IdSalao INT NOT NULL UNIQUE,
    Cnpj VARCHAR(14) NOT NULL,
    RazaoSocial VARCHAR(150) NOT NULL,
    InscricaoEstadual VARCHAR(20) NULL,
    InscricaoMunicipal VARCHAR(20) NULL,
    Ambiente INT NOT NULL DEFAULT 2, -- 1: Produção, 2: Homologação
    CodigoMunicipioIBGE INT NOT NULL,
    CodigoUFIBGE INT NOT NULL,
    RegimeTributario INT NOT NULL, -- 1: Simples Nacional, 3: Regime Normal, etc.
    
    -- Gestão do Certificado A1
    CertificadoPfx VARBINARY(MAX) NULL,
    CertificadoSenha VARBINARY(500) NULL, -- Senha guardada já encriptada (AES-256)
    CertificadoValidade DATETIME NULL,
    
    DataAtualizacao DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT FK_ConfigFiscal_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao)
);
GO

-- TABELA: Armazenamento e Status de Notas Fiscais Emitidas
CREATE TABLE CorteCor_NotaFiscal (
    IdNotaFiscal UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    IdSalao INT NOT NULL,
    IdAgendamento INT NULL, -- Vínculo com o serviço prestado
    IdVendaProduto INT NULL, -- Vínculo com venda na recepção
    
    TipoNota VARCHAR(10) NOT NULL, -- 'NFC-e' ou 'NFS-e'
    Ambiente INT NOT NULL, -- 1: Produção, 2: Homologação
    Numero INT NOT NULL,
    Serie INT NOT NULL,
    ValorTotal DECIMAL(18,2) NOT NULL,
    
    Status VARCHAR(20) NOT NULL, -- 'Pendente', 'Processando', 'Autorizada', 'Rejeitada', 'Cancelada'
    
    ChaveAcesso VARCHAR(44) NULL, -- Presente nas NFC-es e no Padrão Nacional NFS-e
    NumeroRecibo VARCHAR(50) NULL, -- Para envios assíncronos
    ProtocoloAutorizacao VARCHAR(50) NULL,
    JustificativaRejeicao VARCHAR(500) NULL, -- Código + Mensagem em caso de erro
    
    -- Arquivos brutos armazenados e prontos para download em C# ou disparo via email
    XmlEnvio NVARCHAR(MAX) NULL,
    XmlRetorno NVARCHAR(MAX) NULL, 
    
    DataEmissao DATETIME NOT NULL,
    DataAtualizacao DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_NotaFiscal_Salao FOREIGN KEY (IdSalao) REFERENCES CorteCor_Salao(IdSalao),
    CONSTRAINT FK_NotaFiscal_Agendamento FOREIGN KEY (IdAgendamento) REFERENCES CorteCor_Agendamento(IdAgendamento)
);
GO

-- Adicionar campos fiscais em Pessoa
ALTER TABLE CorteCor_Pessoa ADD CpfCnpj VARCHAR(20) NULL;
ALTER TABLE CorteCor_Pessoa ADD InscricaoEstadual VARCHAR(20) NULL;
ALTER TABLE CorteCor_Pessoa ADD InscricaoMunicipal VARCHAR(20) NULL;
ALTER TABLE CorteCor_Pessoa ADD Cep VARCHAR(10) NULL;
ALTER TABLE CorteCor_Pessoa ADD Logradouro VARCHAR(150) NULL;
ALTER TABLE CorteCor_Pessoa ADD Numero VARCHAR(10) NULL;
ALTER TABLE CorteCor_Pessoa ADD Complemento VARCHAR(50) NULL;
ALTER TABLE CorteCor_Pessoa ADD Bairro VARCHAR(100) NULL;
ALTER TABLE CorteCor_Pessoa ADD Cidade VARCHAR(100) NULL;
ALTER TABLE CorteCor_Pessoa ADD UF VARCHAR(2) NULL;
GO

-- Adicionar campos fiscais em Servico
ALTER TABLE CorteCor_Servico ADD CodigoTributacaoMunicipio VARCHAR(20) NULL;
ALTER TABLE CorteCor_Servico ADD Cnae VARCHAR(20) NULL;
ALTER TABLE CorteCor_Servico ADD AliquotaISS DECIMAL(5,2) NULL;
GO