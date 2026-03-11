-- Script: 04_create_log_acessos.sql
-- Descrição: Criação da tabela CorteCor_LogAcessos para auditoria de tentativas de login.

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CorteCor_LogAcessos]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CorteCor_LogAcessos] (
        [Id]              INT IDENTITY(1,1) PRIMARY KEY,
        [Usuario]         NVARCHAR(200)  NOT NULL,
        [DataHora]        DATETIME       NOT NULL DEFAULT GETDATE(),
        [IP_Origem]       VARCHAR(45)    NOT NULL,
        [CredencialUsada] NVARCHAR(200)  NULL,
        [Sucesso]         BIT            NOT NULL DEFAULT 1
    );
    PRINT 'Tabela CorteCor_LogAcessos criada com sucesso!';
END
ELSE
BEGIN
    PRINT 'Tabela CorteCor_LogAcessos já existe.';
END
GO
