-- Script para desativar Meios de Pagamento duplicados (deixando apenas 1 ativo por Salão)
-- Regra: Apenas o meio de pagamento ativo mais recente de cada salão continuará ativo.

WITH CTE_MeiosAtivos AS (
    SELECT 
        IdMeioPagamento,
        IdSalao,
        Ativo,
        ROW_NUMBER() OVER (PARTITION BY IdSalao ORDER BY IdMeioPagamento DESC) as RowNum
    FROM CorteCor_MeioPagamento
    WHERE Ativo = 1
)
UPDATE CorteCor_MeioPagamento
SET Ativo = 0
WHERE IdMeioPagamento IN (
    SELECT IdMeioPagamento 
    FROM CTE_MeiosAtivos 
    WHERE RowNum > 1
);
