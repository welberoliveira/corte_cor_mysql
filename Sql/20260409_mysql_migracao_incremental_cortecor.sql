-- Script incremental para bases ja existentes do CorteCor
-- Objetivo:
-- 1. Aplicar as correcoes que antes dependiam de DROP/CREATE
-- 2. Ajustar colunas UUID para uso com triggers
-- 3. Complementar a estrutura de CorteCor_Pagamento sem perder dados
-- 4. Recriar apenas triggers, sem excluir tabelas
--
-- Observacao:
-- Execute este script no schema de destino ja selecionado.
-- Exemplo: USE cortecordaxa;

SET @OLD_FOREIGN_KEY_CHECKS = @@FOREIGN_KEY_CHECKS;
SET FOREIGN_KEY_CHECKS = 0;

DELIMITER $$

DROP PROCEDURE IF EXISTS `cc_exec`$$
CREATE PROCEDURE `cc_exec`(IN p_sql LONGTEXT)
BEGIN
  SET @cc_sql = p_sql;
  PREPARE stmt FROM @cc_sql;
  EXECUTE stmt;
  DEALLOCATE PREPARE stmt;
END$$

DROP PROCEDURE IF EXISTS `cc_run_if_table_exists`$$
CREATE PROCEDURE `cc_run_if_table_exists`(IN p_table VARCHAR(64), IN p_sql LONGTEXT)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table
  ) THEN
    CALL cc_exec(p_sql);
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_run_if_two_tables_exist`$$
CREATE PROCEDURE `cc_run_if_two_tables_exist`(
  IN p_table_1 VARCHAR(64),
  IN p_table_2 VARCHAR(64),
  IN p_sql LONGTEXT
)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table_1
  )
  AND EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table_2
  ) THEN
    CALL cc_exec(p_sql);
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_add_column_if_missing`$$
CREATE PROCEDURE `cc_add_column_if_missing`(
  IN p_table VARCHAR(64),
  IN p_column VARCHAR(64),
  IN p_definition LONGTEXT
)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table
  )
  AND NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = p_table
      AND column_name = p_column
  ) THEN
    CALL cc_exec(CONCAT('ALTER TABLE `', p_table, '` ADD COLUMN ', p_definition));
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_modify_column_if_exists`$$
CREATE PROCEDURE `cc_modify_column_if_exists`(
  IN p_table VARCHAR(64),
  IN p_column VARCHAR(64),
  IN p_definition LONGTEXT
)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = p_table
      AND column_name = p_column
  ) THEN
    CALL cc_exec(CONCAT('ALTER TABLE `', p_table, '` MODIFY COLUMN ', p_definition));
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_add_index_if_missing`$$
CREATE PROCEDURE `cc_add_index_if_missing`(
  IN p_table VARCHAR(64),
  IN p_index VARCHAR(64),
  IN p_index_definition LONGTEXT
)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table
  )
  AND NOT EXISTS (
    SELECT 1
    FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = p_table
      AND index_name = p_index
  ) THEN
    CALL cc_exec(CONCAT('ALTER TABLE `', p_table, '` ADD INDEX `', p_index, '` ', p_index_definition));
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_add_fk_if_missing_when_clean`$$
CREATE PROCEDURE `cc_add_fk_if_missing_when_clean`(
  IN p_table VARCHAR(64),
  IN p_constraint VARCHAR(64),
  IN p_column VARCHAR(64),
  IN p_ref_table VARCHAR(64),
  IN p_ref_column VARCHAR(64)
)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table
  )
  AND EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_ref_table
  )
  AND EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = p_table
      AND column_name = p_column
  )
  AND EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = p_ref_table
      AND column_name = p_ref_column
  )
  AND NOT EXISTS (
    SELECT 1
    FROM information_schema.referential_constraints
    WHERE constraint_schema = DATABASE()
      AND table_name = p_table
      AND constraint_name = p_constraint
  ) THEN
    SET @cc_orphan_count = 0;
    SET @cc_orphan_sql = CONCAT(
      'SELECT COUNT(*) INTO @cc_orphan_count ',
      'FROM `', p_table, '` t ',
      'LEFT JOIN `', p_ref_table, '` r ON r.`', p_ref_column, '` = t.`', p_column, '` ',
      'WHERE t.`', p_column, '` IS NOT NULL ',
      'AND r.`', p_ref_column, '` IS NULL'
    );

    PREPARE stmt FROM @cc_orphan_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;

    IF COALESCE(@cc_orphan_count, 0) = 0 THEN
      CALL cc_exec(CONCAT(
        'ALTER TABLE `', p_table, '` ',
        'ADD CONSTRAINT `', p_constraint, '` ',
        'FOREIGN KEY (`', p_column, '`) REFERENCES `', p_ref_table, '` (`', p_ref_column, '`)'
      ));
    ELSE
      SELECT CONCAT(
        'Aviso: a FK ', p_constraint, ' nao foi criada porque existem ',
        @cc_orphan_count, ' registro(s) orfaos em ', p_table, '.', p_column
      ) AS Aviso;
    END IF;
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_insert_diag_if_table_exists`$$
CREATE PROCEDURE `cc_insert_diag_if_table_exists`(
  IN p_label VARCHAR(255),
  IN p_table VARCHAR(64),
  IN p_condition LONGTEXT
)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table
  ) THEN
    SET @cc_diag_sql = CONCAT(
      'INSERT INTO `cc_diagnostico` (`Verificacao`, `Quantidade`) ',
      'SELECT ', QUOTE(p_label), ', COUNT(*) ',
      'FROM `', p_table, '` ',
      'WHERE ', p_condition
    );
    PREPARE stmt FROM @cc_diag_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  ELSE
    INSERT INTO `cc_diagnostico` (`Verificacao`, `Quantidade`)
    VALUES (CONCAT(p_label, ' (tabela ausente)'), NULL);
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_insert_diag_pagamento_meio_invalido`$$
CREATE PROCEDURE `cc_insert_diag_pagamento_meio_invalido`()
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'CorteCor_Pagamento'
  )
  AND EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = 'CorteCor_MeioPagamento'
  )
  AND EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'CorteCor_Pagamento'
      AND column_name = 'IdMeioPagamento'
  )
  AND EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'CorteCor_MeioPagamento'
      AND column_name = 'IdMeioPagamento'
  ) THEN
    INSERT INTO `cc_diagnostico` (`Verificacao`, `Quantidade`)
    SELECT 'CorteCor_Pagamento com IdMeioPagamento invalido', COUNT(*)
    FROM `CorteCor_Pagamento` p
    LEFT JOIN `CorteCor_MeioPagamento` m
      ON m.`IdMeioPagamento` = p.`IdMeioPagamento`
    WHERE p.`IdMeioPagamento` IS NOT NULL
      AND m.`IdMeioPagamento` IS NULL;
  ELSE
    INSERT INTO `cc_diagnostico` (`Verificacao`, `Quantidade`)
    VALUES ('CorteCor_Pagamento com IdMeioPagamento invalido (tabela ou coluna ausente)', NULL);
  END IF;
END$$

DROP PROCEDURE IF EXISTS `cc_recreate_uuid_trigger`$$
CREATE PROCEDURE `cc_recreate_uuid_trigger`(
  IN p_trigger VARCHAR(64),
  IN p_table VARCHAR(64),
  IN p_column VARCHAR(64)
)
BEGIN
  IF EXISTS (
    SELECT 1
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = p_table
  ) THEN
    CALL cc_exec(CONCAT('DROP TRIGGER IF EXISTS `', p_trigger, '`'));

    SET @cc_trigger_sql = CONCAT(
      'CREATE TRIGGER `', p_trigger, '` ',
      'BEFORE INSERT ON `', p_table, '` ',
      'FOR EACH ROW ',
      'BEGIN ',
      '  IF NEW.`', p_column, '` IS NULL OR NEW.`', p_column, '` = '''' THEN ',
      '    SET NEW.`', p_column, '` = UUID(); ',
      '  END IF; ',
      'END'
    );

    PREPARE stmt FROM @cc_trigger_sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END$$

DELIMITER ;

-- Ajustes de colunas UUID / chaves tecnicas
CALL cc_add_column_if_missing('CorteCor_ConfigApi', 'ApiKey', '`ApiKey` char(36) DEFAULT NULL');
CALL cc_modify_column_if_exists('CorteCor_ConfigApi', 'ApiKey', '`ApiKey` char(36) DEFAULT NULL');

CALL cc_modify_column_if_exists('CorteCor_NotaFiscal', 'IdNotaFiscal', '`IdNotaFiscal` char(36) NOT NULL');
CALL cc_modify_column_if_exists('CorteCor_NotaFiscalEvento', 'IdEvento', '`IdEvento` char(36) NOT NULL');
CALL cc_modify_column_if_exists('CorteCor_NotaFiscalInutilizacao', 'IdInutilizacao', '`IdInutilizacao` char(36) NOT NULL');
CALL cc_modify_column_if_exists('CorteCor_Pagamento', 'IdPagamento', '`IdPagamento` char(36) NOT NULL');
CALL cc_modify_column_if_exists('CorteCor_SalaoConfigFiscal', 'IdConfigFiscal', '`IdConfigFiscal` char(36) NOT NULL');

-- Complementos de estrutura em CorteCor_Pagamento
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'IdMeioPagamento', '`IdMeioPagamento` int DEFAULT NULL');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'Data', '`Data` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'Descricao', '`Descricao` varchar(400) DEFAULT NULL');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'Contos', '`Contos` varchar(255) DEFAULT NULL');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'Campos', '`Campos` longtext');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'CriadoEm', '`CriadoEm` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'AtualizadoEm', '`AtualizadoEm` datetime(6) DEFAULT NULL');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'PagoEm', '`PagoEm` datetime(6) DEFAULT NULL');
CALL cc_add_column_if_missing('CorteCor_Pagamento', 'MercadoPagoPaymentId', '`MercadoPagoPaymentId` varchar(60) DEFAULT NULL');

CALL cc_add_index_if_missing('CorteCor_Pagamento', 'IX_CorteCor_Pagamento_IdMeioPagamento', '(`IdMeioPagamento`)');
CALL cc_add_index_if_missing('CorteCor_Pagamento', 'IX_CorteCor_Pagamento_MercadoPagoPaymentId', '(`MercadoPagoPaymentId`)');

-- Backfill / normalizacao de dados
CALL cc_run_if_table_exists(
  'CorteCor_ConfigApi',
  'UPDATE `CorteCor_ConfigApi`
      SET `ApiKey` = UUID()
    WHERE `ApiKey` IS NULL
       OR TRIM(`ApiKey`) = '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_NotaFiscal',
  'UPDATE `CorteCor_NotaFiscal`
      SET `IdNotaFiscal` = UUID()
    WHERE `IdNotaFiscal` IS NULL
       OR TRIM(`IdNotaFiscal`) = '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_NotaFiscalEvento',
  'UPDATE `CorteCor_NotaFiscalEvento`
      SET `IdEvento` = UUID()
    WHERE `IdEvento` IS NULL
       OR TRIM(`IdEvento`) = '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_NotaFiscalInutilizacao',
  'UPDATE `CorteCor_NotaFiscalInutilizacao`
      SET `IdInutilizacao` = UUID()
    WHERE `IdInutilizacao` IS NULL
       OR TRIM(`IdInutilizacao`) = '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_SalaoConfigFiscal',
  'UPDATE `CorteCor_SalaoConfigFiscal`
      SET `IdConfigFiscal` = UUID()
    WHERE `IdConfigFiscal` IS NULL
       OR TRIM(`IdConfigFiscal`) = '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_Pagamento',
  'UPDATE `CorteCor_Pagamento`
      SET `IdPagamento` = UUID()
    WHERE `IdPagamento` IS NULL
       OR TRIM(`IdPagamento`) = '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_Pagamento',
  'UPDATE `CorteCor_Pagamento`
      SET `IdMeioPagamento` = NULL
    WHERE `IdMeioPagamento` = 0'
);

CALL cc_run_if_table_exists(
  'CorteCor_Pagamento',
  'UPDATE `CorteCor_Pagamento`
      SET `Data` = COALESCE(`Data`, `CriadoEm`, CURRENT_TIMESTAMP(6))
    WHERE `Data` IS NULL'
);

CALL cc_run_if_table_exists(
  'CorteCor_Pagamento',
  'UPDATE `CorteCor_Pagamento`
      SET `AtualizadoEm` = COALESCE(`AtualizadoEm`, `CriadoEm`, `Data`, CURRENT_TIMESTAMP(6))
    WHERE `AtualizadoEm` IS NULL'
);

CALL cc_run_if_table_exists(
  'CorteCor_Pagamento',
  'UPDATE `CorteCor_Pagamento`
      SET `Contos` = `Descricao`
    WHERE (`Contos` IS NULL OR `Contos` = '''')
      AND `Descricao` IS NOT NULL
      AND `Descricao` <> '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_Pagamento',
  'UPDATE `CorteCor_Pagamento`
      SET `Descricao` = `Contos`
    WHERE (`Descricao` IS NULL OR `Descricao` = '''')
      AND `Contos` IS NOT NULL
      AND `Contos` <> '''''
);

CALL cc_run_if_table_exists(
  'CorteCor_Pagamento',
  'UPDATE `CorteCor_Pagamento`
      SET `PagoEm` = COALESCE(`PagoEm`, `AtualizadoEm`, `CriadoEm`, `Data`)
    WHERE `Status` = ''Pago''
      AND `PagoEm` IS NULL'
);

CALL cc_run_if_two_tables_exist(
  'CorteCor_Pagamento',
  'CorteCor_MeioPagamento',
  'UPDATE `CorteCor_Pagamento` p
      LEFT JOIN `CorteCor_MeioPagamento` m
        ON m.`IdMeioPagamento` = p.`IdMeioPagamento`
       SET p.`IdMeioPagamento` = NULL
    WHERE p.`IdMeioPagamento` IS NOT NULL
      AND m.`IdMeioPagamento` IS NULL'
);

-- FKs somente quando a base estiver limpa
CALL cc_add_fk_if_missing_when_clean('CorteCor_ConfigApi', 'FK_ConfigApi_Salao', 'IdSalao', 'CorteCor_Salao', 'IdSalao');
CALL cc_add_fk_if_missing_when_clean('CorteCor_NotaFiscal', 'FK_NotaFiscal_Salao', 'IdSalao', 'CorteCor_Salao', 'IdSalao');
CALL cc_add_fk_if_missing_when_clean('CorteCor_NotaFiscal', 'FK_NotaFiscal_Agendamento', 'IdAgendamento', 'CorteCor_Agendamento', 'IdAgendamento');
CALL cc_add_fk_if_missing_when_clean('CorteCor_NotaFiscalEvento', 'FK_Evento_NotaFiscal', 'IdNotaFiscal', 'CorteCor_NotaFiscal', 'IdNotaFiscal');
CALL cc_add_fk_if_missing_when_clean('CorteCor_NotaFiscalInutilizacao', 'FK_Inutilizacao_Salao', 'IdSalao', 'CorteCor_Salao', 'IdSalao');
CALL cc_add_fk_if_missing_when_clean('CorteCor_Pagamento', 'FK_CorteCor_Pagamento_Agendamento', 'IdAgendamento', 'CorteCor_Agendamento', 'IdAgendamento');
CALL cc_add_fk_if_missing_when_clean('CorteCor_Pagamento', 'FK_CorteCor_Pagamento_MeioPagamento', 'IdMeioPagamento', 'CorteCor_MeioPagamento', 'IdMeioPagamento');
CALL cc_add_fk_if_missing_when_clean('CorteCor_SalaoConfigFiscal', 'FK_ConfigFiscal_Salao', 'IdSalao', 'CorteCor_Salao', 'IdSalao');

-- Triggers para substituir DEFAULT(UUID()) de forma compativel com MySQL
DELIMITER $$
CALL cc_recreate_uuid_trigger('BI_CorteCor_ConfigApi_SetApiKey', 'CorteCor_ConfigApi', 'ApiKey')$$
CALL cc_recreate_uuid_trigger('BI_CorteCor_NotaFiscal_SetIdNotaFiscal', 'CorteCor_NotaFiscal', 'IdNotaFiscal')$$
CALL cc_recreate_uuid_trigger('BI_CorteCor_NotaFiscalEvento_SetIdEvento', 'CorteCor_NotaFiscalEvento', 'IdEvento')$$
CALL cc_recreate_uuid_trigger('BI_CorteCor_NotaFiscalInutilizacao_SetIdInutilizacao', 'CorteCor_NotaFiscalInutilizacao', 'IdInutilizacao')$$
CALL cc_recreate_uuid_trigger('BI_CorteCor_Pagamento_SetIdPagamento', 'CorteCor_Pagamento', 'IdPagamento')$$
CALL cc_recreate_uuid_trigger('BI_CorteCor_SalaoConfigFiscal_SetIdConfigFiscal', 'CorteCor_SalaoConfigFiscal', 'IdConfigFiscal')$$
DELIMITER ;

-- Diagnostico final
DROP TEMPORARY TABLE IF EXISTS `cc_diagnostico`;
CREATE TEMPORARY TABLE `cc_diagnostico` (
  `Verificacao` varchar(255) NOT NULL,
  `Quantidade` bigint NULL
);

CALL cc_insert_diag_if_table_exists(
  'CorteCor_ConfigApi sem ApiKey',
  'CorteCor_ConfigApi',
  '`ApiKey` IS NULL OR TRIM(`ApiKey`) = '''''
);

CALL cc_insert_diag_if_table_exists(
  'CorteCor_Pagamento sem IdPagamento',
  'CorteCor_Pagamento',
  '`IdPagamento` IS NULL OR TRIM(`IdPagamento`) = '''''
);

CALL cc_insert_diag_pagamento_meio_invalido();

SELECT `Verificacao`, `Quantidade`
FROM `cc_diagnostico`;

DROP TEMPORARY TABLE IF EXISTS `cc_diagnostico`;

-- Limpeza dos helpers
DROP PROCEDURE IF EXISTS `cc_insert_diag_pagamento_meio_invalido`;
DROP PROCEDURE IF EXISTS `cc_insert_diag_if_table_exists`;
DROP PROCEDURE IF EXISTS `cc_recreate_uuid_trigger`;
DROP PROCEDURE IF EXISTS `cc_add_fk_if_missing_when_clean`;
DROP PROCEDURE IF EXISTS `cc_add_index_if_missing`;
DROP PROCEDURE IF EXISTS `cc_modify_column_if_exists`;
DROP PROCEDURE IF EXISTS `cc_add_column_if_missing`;
DROP PROCEDURE IF EXISTS `cc_run_if_two_tables_exist`;
DROP PROCEDURE IF EXISTS `cc_run_if_table_exists`;
DROP PROCEDURE IF EXISTS `cc_exec`;

SET FOREIGN_KEY_CHECKS = @OLD_FOREIGN_KEY_CHECKS;
