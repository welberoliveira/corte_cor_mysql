-- Corrige os nomes dos grupos do plano de contas exibidos no select.
-- Este script recompoe os espacos por Codigo, inclusive se o script anterior
-- tiver deixado nomes como "Ativocirculante".
-- Mantem siglas em caixa alta: CMV, CPV, CSP, CRM, IRPJ, CSLL.

DROP TEMPORARY TABLE IF EXISTS Tmp_GruposPlanoContasNomeCorreto;

CREATE TEMPORARY TABLE Tmp_GruposPlanoContasNomeCorreto (
    Codigo VARCHAR(30) NOT NULL PRIMARY KEY,
    NomeCorreto VARCHAR(200) NOT NULL
);

INSERT INTO Tmp_GruposPlanoContasNomeCorreto (Codigo, NomeCorreto) VALUES
('1.1', 'Ativo Circulante'),
('1.2', 'Ativo Nao Circulante'),
('2.1', 'Passivo Circulante'),
('2.2', 'Passivo Nao Circulante'),
('2.3', 'Marketing CRM'),
('3.1', 'Capital Social'),
('3.2', 'Reservas'),
('3.3', 'Resultados Acumulados'),
('3.4', 'Distribuicoes e Retiradas'),
('4.1', 'Receita Bruta Operacional'),
('5.1', 'Cancelamentos, Devolucoes e Abatimentos'),
('5.2', 'Tributos sobre Vendas e Servicos'),
('6.1', 'Custo das Mercadorias Vendidas - CMV'),
('6.2', 'Custo dos Produtos Vendidos - CPV'),
('6.3', 'Custo dos Servicos Prestados - CSP'),
('7.1', 'Despesas Comerciais / Vendas'),
('7.2', 'Despesas Administrativas'),
('7.3', 'Despesas com Pessoal'),
('7.4', 'Despesas Operacionais Gerais'),
('8.1', 'Receitas Financeiras'),
('8.2', 'Despesas Financeiras'),
('9.1', 'Outras Receitas Operacionais'),
('9.2', 'Outras Despesas Operacionais'),
('10.1', 'Tributos sobre o Lucro'),
('10.2', 'Participacoes');

-- Previa do que sera corrigido.
SELECT
    P.IdSalao,
    P.Codigo,
    COALESCE(NULLIF(P.Nome, ''), P.Descricao) AS NomeAtual,
    T.NomeCorreto AS NomeNovo
FROM CorteCor_PlanoContas P
INNER JOIN Tmp_GruposPlanoContasNomeCorreto T ON T.Codigo = P.Codigo
WHERE P.IdPlano > 0
  AND COALESCE(NULLIF(P.Nivel, 0), 1 + LENGTH(P.Codigo) - LENGTH(REPLACE(P.Codigo, '.', ''))) = 2
  AND (
      COALESCE(P.Nome, '') <> T.NomeCorreto
      OR COALESCE(P.Descricao, '') <> T.NomeCorreto
  )
ORDER BY P.IdSalao, P.Codigo;

UPDATE CorteCor_PlanoContas P
INNER JOIN Tmp_GruposPlanoContasNomeCorreto T ON T.Codigo = P.Codigo
SET
    P.Nome = T.NomeCorreto,
    P.Descricao = T.NomeCorreto
WHERE P.IdPlano > 0
  AND COALESCE(NULLIF(P.Nivel, 0), 1 + LENGTH(P.Codigo) - LENGTH(REPLACE(P.Codigo, '.', ''))) = 2
  AND (
      COALESCE(P.Nome, '') <> T.NomeCorreto
      OR COALESCE(P.Descricao, '') <> T.NomeCorreto
  );

-- Validacao final dos grupos que aparecem no campo "Grupo do Plano de Contas".
SELECT
    IdSalao,
    Codigo,
    Nome,
    Descricao,
    Tipo
FROM CorteCor_PlanoContas
WHERE IdPlano > 0
  AND COALESCE(NULLIF(Nivel, 0), 1 + LENGTH(Codigo) - LENGTH(REPLACE(Codigo, '.', ''))) = 2
ORDER BY IdSalao, Codigo;

DROP TEMPORARY TABLE IF EXISTS Tmp_GruposPlanoContasNomeCorreto;
