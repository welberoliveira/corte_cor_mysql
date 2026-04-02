-- Script para inclusão de campos para a NFS-e Padrão Nacional
-- Executar este script no SQL Server

ALTER TABLE CorteCor_NotaFiscal
ADD ChaveAcessoNacional NVARCHAR(60) NULL;

ALTER TABLE CorteCor_NotaFiscal
ADD NumeroNFSeNacional NVARCHAR(30) NULL;
