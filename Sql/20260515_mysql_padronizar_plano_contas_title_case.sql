-- Padroniza os nomes do plano de contas em maiusculas/minusculas.
-- Mantem siglas contabeis e tecnicas em caixa alta.
-- Seguro para executar com safe update mode: o UPDATE usa P.IdPlano > 0.

DROP FUNCTION IF EXISTS CorteCor_TitleCasePlanoContas;

DELIMITER $$
CREATE FUNCTION CorteCor_TitleCasePlanoContas(p_text TEXT)
RETURNS TEXT
DETERMINISTIC
BEGIN
    DECLARE i INT DEFAULT 1;
    DECLARE texto TEXT DEFAULT '';
    DECLARE resultado TEXT DEFAULT '';
    DECLARE ch VARCHAR(1);
    DECLARE capitalizar TINYINT DEFAULT 1;

    SET texto = LOWER(TRIM(COALESCE(p_text, '')));

    WHILE i <= CHAR_LENGTH(texto) DO
        SET ch = SUBSTRING(texto, i, 1);

        IF capitalizar = 1 AND ch REGEXP '[[:alpha:]]' THEN
            SET resultado = CONCAT(resultado, UPPER(ch));
            SET capitalizar = 0;
        ELSE
            SET resultado = CONCAT(resultado, ch);
        END IF;

        IF ch IN (' ', '/', '-', '(', '[', '{', CHAR(9), CHAR(10), CHAR(13)) THEN
            SET capitalizar = 1;
        ELSEIF ch REGEXP '[[:alnum:]]' THEN
            SET capitalizar = 0;
        END IF;

        SET i = i + 1;
    END WHILE;

    -- Preposicoes, artigos e conectores em minusculo quando aparecem no meio do nome.
    SET resultado = CONCAT(' ', resultado, ' ');
    SET resultado = REPLACE(resultado, ' A ', ' a ');
    SET resultado = REPLACE(resultado, ' Ao ', ' ao ');
    SET resultado = REPLACE(resultado, ' Aos ', ' aos ');
    SET resultado = REPLACE(resultado, ' As ', ' as ');
    SET resultado = REPLACE(resultado, ' Com ', ' com ');
    SET resultado = REPLACE(resultado, ' Da ', ' da ');
    SET resultado = REPLACE(resultado, ' Das ', ' das ');
    SET resultado = REPLACE(resultado, ' De ', ' de ');
    SET resultado = REPLACE(resultado, ' Do ', ' do ');
    SET resultado = REPLACE(resultado, ' Dos ', ' dos ');
    SET resultado = REPLACE(resultado, ' E ', ' e ');
    SET resultado = REPLACE(resultado, ' Em ', ' em ');
    SET resultado = REPLACE(resultado, ' Na ', ' na ');
    SET resultado = REPLACE(resultado, ' Nas ', ' nas ');
    SET resultado = REPLACE(resultado, ' No ', ' no ');
    SET resultado = REPLACE(resultado, ' Nos ', ' nos ');
    SET resultado = REPLACE(resultado, ' Para ', ' para ');
    SET resultado = REPLACE(resultado, ' Por ', ' por ');
    SET resultado = REPLACE(resultado, ' Sobre ', ' sobre ');
    SET resultado = TRIM(resultado);

    -- Correcoes de termos compostos comuns.
    SET resultado = REPLACE(resultado, 'E-Mails', 'E-mails');
    SET resultado = REPLACE(resultado, 'E-Mail', 'E-mail');

    -- Siglas que devem permanecer em caixa alta.
    SET resultado = REPLACE(resultado, 'Apis', 'APIs');
    SET resultado = REPLACE(resultado, 'Api', 'API');
    SET resultado = REPLACE(resultado, 'Cmv', 'CMV');
    SET resultado = REPLACE(resultado, 'Cpv', 'CPV');
    SET resultado = REPLACE(resultado, 'Csp', 'CSP');
    SET resultado = REPLACE(resultado, 'Crm', 'CRM');
    SET resultado = REPLACE(resultado, 'Dce', 'DCE');
    SET resultado = REPLACE(resultado, 'Dre', 'DRE');
    SET resultado = REPLACE(resultado, 'Fgts', 'FGTS');
    SET resultado = REPLACE(resultado, 'Icms', 'ICMS');
    SET resultado = REPLACE(resultado, 'Inss', 'INSS');
    SET resultado = REPLACE(resultado, 'Iof', 'IOF');
    SET resultado = REPLACE(resultado, 'Ipi', 'IPI');
    SET resultado = REPLACE(resultado, 'Iptu', 'IPTU');
    SET resultado = REPLACE(resultado, 'Irpj', 'IRPJ');
    SET resultado = REPLACE(resultado, 'Irrf', 'IRRF');
    SET resultado = REPLACE(resultado, 'Iss', 'ISS');
    SET resultado = REPLACE(resultado, 'Lgpd', 'LGPD');
    SET resultado = REPLACE(resultado, 'Pdf', 'PDF');
    SET resultado = REPLACE(resultado, 'Pis', 'PIS');
    SET resultado = REPLACE(resultado, 'Pix', 'PIX');
    SET resultado = REPLACE(resultado, 'Seo', 'SEO');
    SET resultado = REPLACE(resultado, 'Sms', 'SMS');
    SET resultado = REPLACE(resultado, 'Url', 'URL');
    SET resultado = REPLACE(resultado, 'Cofins', 'COFINS');
    SET resultado = REPLACE(resultado, 'Cprb', 'CPRB');
    SET resultado = REPLACE(resultado, 'Csll', 'CSLL');

    RETURN resultado;
END$$
DELIMITER ;

DROP TEMPORARY TABLE IF EXISTS Tmp_PlanoContasNomeFormatado;
CREATE TEMPORARY TABLE Tmp_PlanoContasNomeFormatado AS
SELECT
    IdPlano,
    CorteCor_TitleCasePlanoContas(COALESCE(NULLIF(Nome, ''), Descricao)) AS NomeFormatado
FROM CorteCor_PlanoContas
WHERE IdPlano > 0
  AND Codigo IS NOT NULL
  AND COALESCE(NULLIF(Nome, ''), Descricao) IS NOT NULL;

ALTER TABLE Tmp_PlanoContasNomeFormatado
    ADD PRIMARY KEY (IdPlano);

-- Previa do que sera alterado.
SELECT
    P.Codigo,
    COALESCE(NULLIF(P.Nome, ''), P.Descricao) AS NomeAtual,
    T.NomeFormatado AS NomeNovo
FROM CorteCor_PlanoContas P
INNER JOIN Tmp_PlanoContasNomeFormatado T ON T.IdPlano = P.IdPlano
WHERE P.IdPlano > 0
  AND (
      COALESCE(P.Nome, '') <> T.NomeFormatado
      OR COALESCE(P.Descricao, '') <> T.NomeFormatado
  )
ORDER BY P.Codigo;

UPDATE CorteCor_PlanoContas P
INNER JOIN Tmp_PlanoContasNomeFormatado T ON T.IdPlano = P.IdPlano
SET
    P.Nome = T.NomeFormatado,
    P.Descricao = T.NomeFormatado
WHERE P.IdPlano > 0
  AND (
      COALESCE(P.Nome, '') <> T.NomeFormatado
      OR COALESCE(P.Descricao, '') <> T.NomeFormatado
  );

-- Validacao dos grupos exibidos no select de lancamentos.
SELECT
    Codigo,
    Nome,
    Descricao,
    Tipo
FROM CorteCor_PlanoContas
WHERE IdPlano > 0
  AND COALESCE(NULLIF(Nivel, 0), 1 + LENGTH(Codigo) - LENGTH(REPLACE(Codigo, '.', ''))) = 2
ORDER BY Codigo;

DROP TEMPORARY TABLE IF EXISTS Tmp_PlanoContasNomeFormatado;
DROP FUNCTION IF EXISTS CorteCor_TitleCasePlanoContas;
