-- Validacao pos-importacao do schema CorteCor em MySQL.
-- Execute no banco de destino apos rodar o script base.
-- Resultado esperado com o script atual:
-- - 49 tabelas CorteCor_*
-- - 6 triggers BI_CorteCor_*
-- - 0 colunas criticas ausentes

SELECT DATABASE() AS BancoAtual;

SELECT
  'Tabelas CorteCor_*' AS Verificacao,
  COUNT(*) AS Quantidade,
  'Esperado: 49' AS Esperado
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name LIKE 'CorteCor|_%' ESCAPE '|';

SELECT
  'Triggers BI_CorteCor_*' AS Verificacao,
  COUNT(*) AS Quantidade,
  'Esperado: 6' AS Esperado
FROM information_schema.triggers
WHERE trigger_schema = DATABASE()
  AND trigger_name LIKE 'BI_CorteCor|_%' ESCAPE '|';

SELECT
  'Colunas criticas ausentes' AS Verificacao,
  COUNT(*) AS Quantidade,
  'Esperado: 0' AS Esperado
FROM (
  SELECT 'CorteCor_PessoaFicha' AS Tabela, 'PessoaID' AS Coluna
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'FichaID'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'Nome'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'CPF'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'DataNascimento'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'ConjugeNome'
  UNION ALL SELECT 'CorteCor_Pagamento', 'IdPagamento'
  UNION ALL SELECT 'CorteCor_Pagamento', 'IdMeioPagamento'
  UNION ALL SELECT 'CorteCor_Pagamento', 'MercadoPagoPaymentId'
  UNION ALL SELECT 'CorteCor_ConfigApi', 'ApiKey'
  UNION ALL SELECT 'CorteCor_NotaFiscal', 'IdNotaFiscal'
  UNION ALL SELECT 'CorteCor_NotaFiscalEvento', 'IdEvento'
  UNION ALL SELECT 'CorteCor_NotaFiscalInutilizacao', 'IdInutilizacao'
  UNION ALL SELECT 'CorteCor_SalaoConfigFiscal', 'IdConfigFiscal'
) esperadas
LEFT JOIN information_schema.columns c
  ON c.table_schema = DATABASE()
 AND c.table_name = esperadas.Tabela
 AND c.column_name = esperadas.Coluna
WHERE c.column_name IS NULL;

SELECT
  esperadas.Tabela,
  esperadas.Coluna,
  CASE
    WHEN c.column_name IS NULL THEN 'AUSENTE'
    ELSE 'OK'
  END AS Status
FROM (
  SELECT 'CorteCor_PessoaFicha' AS Tabela, 'PessoaID' AS Coluna
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'FichaID'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'Nome'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'CPF'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'DataNascimento'
  UNION ALL SELECT 'CorteCor_PessoaFicha', 'ConjugeNome'
  UNION ALL SELECT 'CorteCor_Pagamento', 'IdPagamento'
  UNION ALL SELECT 'CorteCor_Pagamento', 'IdMeioPagamento'
  UNION ALL SELECT 'CorteCor_Pagamento', 'MercadoPagoPaymentId'
  UNION ALL SELECT 'CorteCor_ConfigApi', 'ApiKey'
  UNION ALL SELECT 'CorteCor_NotaFiscal', 'IdNotaFiscal'
  UNION ALL SELECT 'CorteCor_NotaFiscalEvento', 'IdEvento'
  UNION ALL SELECT 'CorteCor_NotaFiscalInutilizacao', 'IdInutilizacao'
  UNION ALL SELECT 'CorteCor_SalaoConfigFiscal', 'IdConfigFiscal'
) esperadas
LEFT JOIN information_schema.columns c
  ON c.table_schema = DATABASE()
 AND c.table_name = esperadas.Tabela
 AND c.column_name = esperadas.Coluna
ORDER BY esperadas.Tabela, esperadas.Coluna;
