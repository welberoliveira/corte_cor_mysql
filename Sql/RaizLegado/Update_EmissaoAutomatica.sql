-- Script para adicionar a flag de Emissão Automática de Nota Fiscal no MySQL/SQL Server

IF COL_LENGTH('CorteCor_SalaoConfigFiscal', 'EmissaoAutomatica') IS NULL 
BEGIN
    ALTER TABLE CorteCor_SalaoConfigFiscal ADD EmissaoAutomatica BIT NOT NULL DEFAULT 0;
END
GO
