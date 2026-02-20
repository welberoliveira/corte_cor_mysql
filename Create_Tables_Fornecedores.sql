-- Tabela para Fornecedores de Email
CREATE TABLE CorteCor_FornecedoresEmail (
    IdFornecedor INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL, -- Ex: Brevo, SendGrid, Amazon SES
    ApiKey VARCHAR(255),
    ApiSecret VARCHAR(255),
    Endpoint VARCHAR(255),
    RemetenteNome VARCHAR(100),
    RemetenteEmail VARCHAR(100),
    Ativo BIT DEFAULT 0, -- Apenas um pode estar ativo por vez
    DataCriacao DATETIME DEFAULT GETDATE(),
    DataAtualizacao DATETIME
);

-- Tabela para Fornecedores de SMS
CREATE TABLE CorteCor_FornecedoresSMS (
    IdFornecedor INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL, -- Ex: Twilio, Brevo, Zenvia
    ApiKey VARCHAR(255),
    ApiSecret VARCHAR(255),
    Endpoint VARCHAR(255),
    Remetente VARCHAR(50), -- Sender ID ou número
    Ativo BIT DEFAULT 0,
    DataCriacao DATETIME DEFAULT GETDATE(),
    DataAtualizacao DATETIME
);

-- Tabela para Fornecedores de Whatsapp
CREATE TABLE CorteCor_FornecedoresWhatsapp (
    IdFornecedor INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL, -- Ex: Twilio, WppConnect, Z-API
    ApiKey VARCHAR(255),
    ApiSecret VARCHAR(255),
    Endpoint VARCHAR(255),
    InstanceId VARCHAR(100), -- Específico de algumas APIs de Whataspp
    Token VARCHAR(255),      -- Token adicional se necessário
    Ativo BIT DEFAULT 0,
    DataCriacao DATETIME DEFAULT GETDATE(),
    DataAtualizacao DATETIME
);

-- Inserir dados iniciais da Brevo (Exemplo - placeholder)
-- O administrador deverá atualizar com as chaves reais via sistema
INSERT INTO CorteCor_FornecedoresEmail (Nome, ApiKey, Endpoint, RemetenteNome, RemetenteEmail, Ativo, DataAtualizacao)
VALUES ('Brevo', 'xkeysib-YOUR_API_KEY_HERE', 'https://api.brevo.com/v3/smtp/email', 'CorteCor', 'no-reply@cortecor.com.br', 1, GETDATE());

INSERT INTO CorteCor_FornecedoresSMS (Nome, ApiKey, Endpoint, Remetente, Ativo, DataAtualizacao)
VALUES ('Brevo', 'xkeysib-YOUR_API_KEY_HERE', 'https://api.brevo.com/v3/transactionalSMS/send', 'CorteCor', 1, GETDATE());

-- Exemplo para Whatsapp (Z-API como placeholder)
INSERT INTO CorteCor_FornecedoresWhatsapp (Nome, Endpoint, InstanceId, Token, Ativo, DataAtualizacao)
VALUES ('Z-API', 'https://api.z-api.io/instances/', 'YOUR_INSTANCE_ID', 'YOUR_TOKEN', 1, GETDATE());
