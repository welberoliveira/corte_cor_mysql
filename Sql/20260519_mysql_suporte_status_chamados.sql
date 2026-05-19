-- Padroniza os status do modulo de suporte.
-- Pode ser executado no MySQL sem apagar chamados existentes.

UPDATE CorteCor_SuporteChamado
SET Status = 'Solicitado'
WHERE Status IN ('Aberto', 'Enviado', 'FalhaEmail');

ALTER TABLE CorteCor_SuporteChamado
    MODIFY Status VARCHAR(80) NOT NULL DEFAULT 'Solicitado';

