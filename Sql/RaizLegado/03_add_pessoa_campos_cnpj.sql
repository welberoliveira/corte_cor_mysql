-- Script: 03_add_pessoa_campos_cnpj.sql
-- Descrição: Adiciona campos RazaoSocial, NomeFantasia e Cnae à tabela CorteCor_Pessoa
--            para armazenar dados da consulta CNPJ (ReceitaWS).

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_Pessoa') AND name = 'RazaoSocial')
BEGIN
    ALTER TABLE CorteCor_Pessoa ADD RazaoSocial NVARCHAR(200) NULL;
    PRINT 'Coluna RazaoSocial adicionada com sucesso.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_Pessoa') AND name = 'NomeFantasia')
BEGIN
    ALTER TABLE CorteCor_Pessoa ADD NomeFantasia NVARCHAR(200) NULL;
    PRINT 'Coluna NomeFantasia adicionada com sucesso.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_Pessoa') AND name = 'Cnae')
BEGIN
    ALTER TABLE CorteCor_Pessoa ADD Cnae VARCHAR(20) NULL;
    PRINT 'Coluna Cnae adicionada com sucesso.';
END
GO

PRINT 'Script 03 executado com sucesso!';
GO
