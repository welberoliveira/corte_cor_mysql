-- Script auxiliar para vincular serviços sem categoria a uma categoria padrão
-- Execute após aplicar o script 11_add_categoria_servico.sql

IF EXISTS (SELECT 1 FROM CorteCor_CategoriaProduto)
BEGIN
    UPDATE CorteCor_Servico 
    SET IdCategoria = (SELECT TOP 1 IdCategoria FROM CorteCor_CategoriaProduto ORDER BY IdCategoria ASC)
    WHERE IdCategoria IS NULL;
    
    PRINT 'Serviços sem categoria foram vinculados à primeira categoria encontrada.';
END
ELSE
BEGIN
    PRINT 'Nenhuma categoria cadastrada. Por favor, cadastre uma categoria antes de executar este script.';
END
