-- Adiciona a classificacao Lead ao cadastro de pessoas.
-- Execute este script no banco MySQL antes de publicar a versao do sistema que usa IsLead.

DELIMITER $$

DROP PROCEDURE IF EXISTS CorteCor_AddPessoaLead $$
CREATE PROCEDURE CorteCor_AddPessoaLead()
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'CorteCor_Pessoa'
          AND COLUMN_NAME = 'IsLead'
    ) THEN
        ALTER TABLE CorteCor_Pessoa
            ADD COLUMN IsLead TINYINT(1) NOT NULL DEFAULT 0 AFTER IsFornecedor;
    END IF;

    ALTER TABLE CorteCor_Pessoa
        MODIFY COLUMN Telefone VARCHAR(300) DEFAULT NULL,
        MODIFY COLUMN Email VARCHAR(600) DEFAULT NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'CorteCor_Pessoa'
          AND INDEX_NAME = 'IX_CorteCor_Pessoa_Salao_Lead_Ativo_Nome'
    ) THEN
        CREATE INDEX IX_CorteCor_Pessoa_Salao_Lead_Ativo_Nome
            ON CorteCor_Pessoa (IdSalao, IsLead, Excluido, Nome);
    END IF;
END $$

CALL CorteCor_AddPessoaLead() $$
DROP PROCEDURE IF EXISTS CorteCor_AddPessoaLead $$

DELIMITER ;

UPDATE CorteCor_Pessoa
SET IsLead = COALESCE(IsLead, 0)
WHERE IdPessoa > 0;
