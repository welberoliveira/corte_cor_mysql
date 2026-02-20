-- Script de inserção para os fornecedores de SMS e Email nas tabelas recém-renomeadas.
-- Substitua os valores YOUR_API_KEY ou YOUR_PASSWORD conforme necessário

-- 1. Inserir Brevo como provedor de E-mail (se não existir, ou atualizar se existir)
IF NOT EXISTS (SELECT 1 FROM CorteCor_FornecedoresEmail WHERE Nome = 'Brevo')
BEGIN
    INSERT INTO CorteCor_FornecedoresEmail (Nome, ApiKey, ApiSecret, Endpoint, RemetenteNome, RemetenteEmail, Ativo, DataCriacao, DataAtualizacao) 
    VALUES ('Brevo', 'YOUR_BREVO_API_KEY', '', 'https://api.brevo.com/v3/smtp/email', 'CorteCor', 'no-reply@cortecor.com.br', 1, GETDATE(), GETDATE());
END
ELSE
BEGIN
    UPDATE CorteCor_FornecedoresEmail 
    SET ApiKey = 'YOUR_BREVO_API_KEY', Ativo = 1, DataAtualizacao = GETDATE()
    WHERE Nome = 'Brevo';
END

-- Desativa todos os outros emails para que só Brevo fique ativo
UPDATE CorteCor_FornecedoresEmail SET Ativo = 0 WHERE Nome != 'Brevo';

GO

-- 2. Inserir SMSMarket como provedor de SMS e definir como ativo
IF NOT EXISTS (SELECT 1 FROM CorteCor_FornecedoresSMS WHERE Nome = 'SMSMarket')
BEGIN
    -- Substitua 'SEU_USUARIO_SMSMARKET' e 'SUA_SENHA_SMSMARKET' pelas chaves corretas que o salão for usar
    INSERT INTO CorteCor_FornecedoresSMS (Nome, ApiKey, ApiSecret, Endpoint, Remetente, Ativo, DataCriacao, DataAtualizacao) 
    VALUES ('SMSMarket', 'SEU_USUARIO_SMSMARKET', 'SUA_SENHA_SMSMARKET', 'https://api.smsmarket.com.br/webservice-rest/send-single', 'CorteCor', 1, GETDATE(), GETDATE());
END
ELSE
BEGIN
    UPDATE CorteCor_FornecedoresSMS
    SET ApiKey = 'SEU_USUARIO_SMSMARKET', ApiSecret = 'SUA_SENHA_SMSMARKET', Ativo = 1, DataAtualizacao = GETDATE()
    WHERE Nome = 'SMSMarket';
END

-- E também vamos garantir que Brevo no SMS não fique como ativo se formos usar SMSMarket
UPDATE CorteCor_FornecedoresSMS SET Ativo = 0 WHERE Nome != 'SMSMarket';

GO
