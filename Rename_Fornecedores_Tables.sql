-- Script para renomear as tabelas de fornecedores
-- Executar no banco de dados

EXEC sp_rename 'FornecedoresEmail', 'CorteCor_FornecedoresEmail';
GO

EXEC sp_rename 'FornecedoresSMS', 'CorteCor_FornecedoresSMS';
GO

EXEC sp_rename 'FornecedoresWhatsapp', 'CorteCor_FornecedoresWhatsapp';
GO
