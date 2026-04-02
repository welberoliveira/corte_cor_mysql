-- Fix: Tornar a coluna 'Cor' nullable na tabela CorteCor_Servico
-- A coluna 'Cor' não é mais utilizada pelo sistema, mas está marcada como NOT NULL no banco,
-- o que impede o cadastro de novos serviços.
-- Este script altera a coluna para permitir NULL, desbloqueando o cadastro.

ALTER TABLE CorteCor_Servico
ALTER COLUMN Cor NVARCHAR(20) NULL;
GO
