USE CorteCor;
GO

-- Adicionar colunas DataInicio e DataFim na tabela de Configuração de Lembretes
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CorteCor_LembreteConfig' AND COLUMN_NAME = 'DataInicio')
BEGIN
    ALTER TABLE CorteCor_LembreteConfig
    ADD DataInicio DATETIME NOT NULL DEFAULT GETDATE();
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CorteCor_LembreteConfig' AND COLUMN_NAME = 'DataFim')
BEGIN
    ALTER TABLE CorteCor_LembreteConfig
    ADD DataFim DATETIME NULL;
END
GO
