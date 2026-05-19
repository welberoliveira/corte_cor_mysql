-- Adiciona o campo Recorrencia em lancamentos financeiros e vendas.
-- Valores esperados pela aplicacao: Nenhuma, Mensal.

SET @schema_name = DATABASE();

SET @sql = (
    SELECT IF(
        EXISTS (
            SELECT 1
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schema_name
              AND TABLE_NAME = 'CorteCor_FinanceiroTitulo'
              AND COLUMN_NAME = 'Recorrencia'
        ),
        'SELECT ''CorteCor_FinanceiroTitulo.Recorrencia ja existe'' AS Info;',
        'ALTER TABLE CorteCor_FinanceiroTitulo ADD COLUMN Recorrencia varchar(20) NOT NULL DEFAULT ''Nenhuma'' AFTER Status;'
    )
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = (
    SELECT IF(
        EXISTS (
            SELECT 1
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schema_name
              AND TABLE_NAME = 'CorteCor_VendaProduto'
              AND COLUMN_NAME = 'Recorrencia'
        ),
        'SELECT ''CorteCor_VendaProduto.Recorrencia ja existe'' AS Info;',
        'ALTER TABLE CorteCor_VendaProduto ADD COLUMN Recorrencia varchar(20) NOT NULL DEFAULT ''Nenhuma'' AFTER TipoPagamento;'
    )
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE CorteCor_FinanceiroTitulo
SET Recorrencia = 'Nenhuma'
WHERE IdTitulo > ''
  AND (Recorrencia IS NULL OR Recorrencia = '');

UPDATE CorteCor_VendaProduto
SET Recorrencia = 'Nenhuma'
WHERE IdVendaProduto > 0
  AND (Recorrencia IS NULL OR Recorrencia = '');

SELECT
    'CorteCor_FinanceiroTitulo' AS Tabela,
    COUNT(*) AS TotalRegistros,
    SUM(CASE WHEN Recorrencia = 'Mensal' THEN 1 ELSE 0 END) AS TotalMensal
FROM CorteCor_FinanceiroTitulo
UNION ALL
SELECT
    'CorteCor_VendaProduto' AS Tabela,
    COUNT(*) AS TotalRegistros,
    SUM(CASE WHEN Recorrencia = 'Mensal' THEN 1 ELSE 0 END) AS TotalMensal
FROM CorteCor_VendaProduto;
