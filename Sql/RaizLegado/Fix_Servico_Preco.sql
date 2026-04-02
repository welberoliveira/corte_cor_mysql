-- Fix: Aumentar o tamanho da coluna 'Preco' na tabela CorteCor_Servico
-- O valor máximo aceito estava sendo 9,99 (provavelmente porque a coluna foi criada como DECIMAL(3, 2)).
-- Este script altera o tipo da coluna para DECIMAL(10, 2), permitindo valores de até 99.999.999,99.

ALTER TABLE CorteCor_Servico 
ALTER COLUMN Preco DECIMAL(10, 2) NOT NULL;
GO
