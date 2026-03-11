-- Script para adicionar campos de Cidade e UF na configuração fiscal
-- CorteCor - Sistema de Gestão de Salões

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'EnderecoCidade')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD EnderecoCidade NVARCHAR(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'EnderecoUF')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD EnderecoUF CHAR(2) NULL;
END
GO
