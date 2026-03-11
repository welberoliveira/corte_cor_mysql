-- Script para adicionar campos faltantes na configuração fiscal do salão
-- CorteCor - Sistema de Gestão de Salões

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'TokenNfse')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD TokenNfse NVARCHAR(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'CSC')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD CSC NVARCHAR(50) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'IdCSC')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD IdCSC NVARCHAR(20) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'SerieNFCe')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD SerieNFCe INT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'NumeroNFCe')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD NumeroNFCe INT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'SerieNFSe')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD SerieNFSe INT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'NumeroNFSe')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD NumeroNFSe INT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'RegimeEspecialTributacao')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD RegimeEspecialTributacao INT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'IssExigibilidade')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD IssExigibilidade INT NOT NULL DEFAULT 1; -- 1: Exigível
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'IssRetido')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD IssRetido INT NOT NULL DEFAULT 2; -- 2: Não Retido
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'EnderecoLogradouro')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD EnderecoLogradouro NVARCHAR(150) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'EnderecoNumero')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD EnderecoNumero NVARCHAR(10) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'EnderecoBairro')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD EnderecoBairro NVARCHAR(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'EnderecoCep')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD EnderecoCep NVARCHAR(10) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'Telefone')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD Telefone NVARCHAR(20) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CorteCor_SalaoConfigFiscal') AND name = 'Email')
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD Email NVARCHAR(100) NULL;
END
GO
