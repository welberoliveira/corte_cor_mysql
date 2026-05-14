-- MySQL/Locaweb - ajuste de pagamento sem agendamento obrigatorio.
-- Reexecutavel e tolerante a importacoes parciais.
-- O vinculo de venda do sistema e CorteCor_VendaProduto.IdVendaProduto.
-- Nao execute sem backup. Este script nao cria dados reais e nao deve ser aplicado automaticamente pela aplicacao.

SET @schema_name := DATABASE();

DROP PROCEDURE IF EXISTS CorteCor_EnsurePagamentoOrigem;

DELIMITER $$

CREATE PROCEDURE CorteCor_EnsurePagamentoOrigem()
BEGIN
  DECLARE fk_agendamento varchar(128) DEFAULT NULL;
  DECLARE fk_salao varchar(128) DEFAULT NULL;
  DECLARE invalid_count bigint DEFAULT 0;
  DECLARE previous_sql_safe_updates int DEFAULT 0;
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    SET SESSION SQL_SAFE_UPDATES = previous_sql_safe_updates;
    RESIGNAL;
  END;

  SET previous_sql_safe_updates = @@SESSION.SQL_SAFE_UPDATES;
  SET SESSION SQL_SAFE_UPDATES = 0;

  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = @schema_name
      AND TABLE_NAME = 'CorteCor_Pagamento'
  ) THEN
    SELECT 'ERRO' AS Status,
           'Tabela CorteCor_Pagamento nao encontrada. Importe/crie a tabela antes deste ajuste.' AS Mensagem;
  ELSE
    SET fk_agendamento = (
      SELECT CONSTRAINT_NAME
      FROM information_schema.KEY_COLUMN_USAGE
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Pagamento'
        AND COLUMN_NAME = 'IdAgendamento'
        AND REFERENCED_TABLE_NAME = 'CorteCor_Agendamento'
      LIMIT 1
    );

    IF fk_agendamento IS NOT NULL THEN
      SET @sql_drop_fk := CONCAT('ALTER TABLE `CorteCor_Pagamento` DROP FOREIGN KEY `', fk_agendamento, '`');
      PREPARE stmt_drop_fk FROM @sql_drop_fk;
      EXECUTE stmt_drop_fk;
      DEALLOCATE PREPARE stmt_drop_fk;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND COLUMN_NAME = 'IdSalao'
    ) THEN
      ALTER TABLE `CorteCor_Pagamento` ADD COLUMN `IdSalao` int DEFAULT NULL;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND COLUMN_NAME = 'IdPedido'
    ) THEN
      ALTER TABLE `CorteCor_Pagamento` ADD COLUMN `IdPedido` int DEFAULT NULL;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND COLUMN_NAME = 'IdVendaProduto'
    ) THEN
      ALTER TABLE `CorteCor_Pagamento` ADD COLUMN `IdVendaProduto` int DEFAULT NULL;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND COLUMN_NAME = 'OrigemPagamento'
    ) THEN
      ALTER TABLE `CorteCor_Pagamento` ADD COLUMN `OrigemPagamento` varchar(30) NOT NULL DEFAULT 'Avulso';
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND COLUMN_NAME = 'IdAgendamento'
    ) THEN
      UPDATE `CorteCor_Pagamento`
      SET `IdAgendamento` = NULL
      WHERE `IdAgendamento` = 0;

      ALTER TABLE `CorteCor_Pagamento`
        MODIFY COLUMN `IdAgendamento` int DEFAULT NULL;

      IF EXISTS (
        SELECT 1 FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'CorteCor_Pagamento'
          AND INDEX_NAME = 'UX_CorteCor_Pagamento_Agendamento_Ativo'
      ) THEN
        ALTER TABLE `CorteCor_Pagamento` DROP INDEX `UX_CorteCor_Pagamento_Agendamento_Ativo`;
      END IF;

      IF EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'CorteCor_Agendamento'
          AND COLUMN_NAME = 'IdAgendamento'
      ) THEN
        UPDATE `CorteCor_Pagamento` P
        LEFT JOIN `CorteCor_Agendamento` A ON A.`IdAgendamento` = P.`IdAgendamento`
        SET P.`IdAgendamento` = NULL
        WHERE P.`IdAgendamento` IS NOT NULL
          AND A.`IdAgendamento` IS NULL;
      ELSE
        UPDATE `CorteCor_Pagamento`
        SET `IdAgendamento` = NULL
        WHERE `IdAgendamento` IS NOT NULL;
      END IF;
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Pedido'
        AND COLUMN_NAME = 'IdPedido'
    ) THEN
      UPDATE `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_Pedido` Ped ON Ped.`IdPedido` = P.`IdPedido`
      SET P.`IdPedido` = NULL
      WHERE P.`IdPedido` IS NOT NULL
        AND Ped.`IdPedido` IS NULL;
    ELSE
      UPDATE `CorteCor_Pagamento`
      SET `IdPedido` = NULL
      WHERE `IdPedido` IS NOT NULL;
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_VendaProduto'
        AND COLUMN_NAME = 'IdVendaProduto'
    ) THEN
      UPDATE `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_VendaProduto` V ON V.`IdVendaProduto` = P.`IdVendaProduto`
      SET P.`IdVendaProduto` = NULL
      WHERE P.`IdVendaProduto` IS NOT NULL
        AND V.`IdVendaProduto` IS NULL;
    ELSE
      UPDATE `CorteCor_Pagamento`
      SET `IdVendaProduto` = NULL
      WHERE `IdVendaProduto` IS NOT NULL;
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Agendamento'
        AND COLUMN_NAME = 'IdAgendamento'
    )
    AND EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Agendamento'
        AND COLUMN_NAME = 'IdServico'
    )
    AND EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Servico'
        AND COLUMN_NAME = 'IdServico'
    )
    AND EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Servico'
        AND COLUMN_NAME = 'IdSalao'
    ) THEN
      UPDATE `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_Agendamento` A ON A.`IdAgendamento` = P.`IdAgendamento`
      LEFT JOIN `CorteCor_Servico` S ON S.`IdServico` = A.`IdServico`
      SET P.`IdSalao` = COALESCE(P.`IdSalao`, S.`IdSalao`)
      WHERE P.`IdAgendamento` IS NOT NULL;
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Pedido'
        AND COLUMN_NAME = 'IdPedido'
    )
    AND EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Pedido'
        AND COLUMN_NAME = 'IdSalao'
    ) THEN
      UPDATE `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_Pedido` Ped ON Ped.`IdPedido` = P.`IdPedido`
      SET P.`IdSalao` = COALESCE(P.`IdSalao`, Ped.`IdSalao`);
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_VendaProduto'
        AND COLUMN_NAME = 'IdVendaProduto'
    )
    AND EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_VendaProduto'
        AND COLUMN_NAME = 'IdSalao'
    ) THEN
      UPDATE `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_VendaProduto` V ON V.`IdVendaProduto` = P.`IdVendaProduto`
      SET P.`IdSalao` = COALESCE(P.`IdSalao`, V.`IdSalao`);
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Salao'
        AND COLUMN_NAME = 'IdSalao'
    ) THEN
      UPDATE `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_Salao` Sal ON Sal.`IdSalao` = P.`IdSalao`
      SET P.`IdSalao` = NULL
      WHERE P.`IdSalao` IS NOT NULL
        AND Sal.`IdSalao` IS NULL;
    END IF;

    UPDATE `CorteCor_Pagamento`
    SET `OrigemPagamento` = CASE
        WHEN `IdAgendamento` IS NOT NULL THEN 'Agendamento'
        WHEN `IdPedido` IS NOT NULL THEN 'Pedido'
        WHEN `IdVendaProduto` IS NOT NULL THEN 'Venda'
        ELSE 'Avulso'
      END;

    ALTER TABLE `CorteCor_Pagamento`
      MODIFY COLUMN `OrigemPagamento` varchar(30) NOT NULL DEFAULT 'Avulso';

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND INDEX_NAME = 'IX_CorteCor_Pagamento_IdSalao'
    ) THEN
      CREATE INDEX `IX_CorteCor_Pagamento_IdSalao` ON `CorteCor_Pagamento` (`IdSalao`);
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND INDEX_NAME = 'IX_CorteCor_Pagamento_IdAgendamento'
    ) THEN
      CREATE INDEX `IX_CorteCor_Pagamento_IdAgendamento` ON `CorteCor_Pagamento` (`IdAgendamento`);
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND INDEX_NAME = 'IX_CorteCor_Pagamento_IdPedido'
    ) THEN
      CREATE INDEX `IX_CorteCor_Pagamento_IdPedido` ON `CorteCor_Pagamento` (`IdPedido`);
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND INDEX_NAME = 'IX_CorteCor_Pagamento_IdVendaProduto'
    ) THEN
      CREATE INDEX `IX_CorteCor_Pagamento_IdVendaProduto` ON `CorteCor_Pagamento` (`IdVendaProduto`);
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'CorteCor_Pagamento' AND INDEX_NAME = 'IX_CorteCor_Pagamento_OrigemPagamento'
    ) THEN
      CREATE INDEX `IX_CorteCor_Pagamento_OrigemPagamento` ON `CorteCor_Pagamento` (`OrigemPagamento`);
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Salao'
        AND COLUMN_NAME = 'IdSalao'
    ) THEN
      SET fk_salao = (
        SELECT CONSTRAINT_NAME
        FROM information_schema.KEY_COLUMN_USAGE
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'CorteCor_Pagamento'
          AND COLUMN_NAME = 'IdSalao'
          AND REFERENCED_TABLE_NAME = 'CorteCor_Salao'
        LIMIT 1
      );

      SELECT COUNT(*)
        INTO invalid_count
      FROM `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_Salao` Sal ON Sal.`IdSalao` = P.`IdSalao`
      WHERE P.`IdSalao` IS NOT NULL
        AND Sal.`IdSalao` IS NULL;

      IF fk_salao IS NULL AND invalid_count = 0 THEN
        ALTER TABLE `CorteCor_Pagamento`
          ADD CONSTRAINT `FK_CorteCor_Pagamento_Salao`
          FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`);
      END IF;
    END IF;

    IF EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA = @schema_name
        AND TABLE_NAME = 'CorteCor_Agendamento'
        AND COLUMN_NAME = 'IdAgendamento'
    ) THEN
      SET fk_agendamento = (
        SELECT CONSTRAINT_NAME
        FROM information_schema.KEY_COLUMN_USAGE
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'CorteCor_Pagamento'
          AND COLUMN_NAME = 'IdAgendamento'
          AND REFERENCED_TABLE_NAME = 'CorteCor_Agendamento'
        LIMIT 1
      );

      SELECT COUNT(*)
        INTO invalid_count
      FROM `CorteCor_Pagamento` P
      LEFT JOIN `CorteCor_Agendamento` A ON A.`IdAgendamento` = P.`IdAgendamento`
      WHERE P.`IdAgendamento` IS NOT NULL
        AND A.`IdAgendamento` IS NULL;

      IF fk_agendamento IS NULL AND invalid_count = 0 THEN
        ALTER TABLE `CorteCor_Pagamento`
          ADD CONSTRAINT `FK_CorteCor_Pagamento_Agendamento`
          FOREIGN KEY (`IdAgendamento`) REFERENCES `CorteCor_Agendamento` (`IdAgendamento`);
      END IF;
    END IF;
  END IF;

  SET SESSION SQL_SAFE_UPDATES = previous_sql_safe_updates;
END$$

DELIMITER ;

CALL CorteCor_EnsurePagamentoOrigem();

DROP PROCEDURE IF EXISTS CorteCor_EnsurePagamentoOrigem;

SELECT
  'Tabela CorteCor_Pagamento' AS Verificacao,
  CASE WHEN EXISTS (
    SELECT 1 FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CorteCor_Pagamento'
  ) THEN 'OK' ELSE 'NAO ENCONTRADA' END AS Resultado
UNION ALL
SELECT
  'Coluna CorteCor_Pagamento.IdVendaProduto',
  CASE WHEN EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CorteCor_Pagamento' AND COLUMN_NAME = 'IdVendaProduto'
  ) THEN 'OK' ELSE 'NAO ENCONTRADA' END
UNION ALL
SELECT
  'Tabela CorteCor_VendaProduto',
  CASE WHEN EXISTS (
    SELECT 1 FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CorteCor_VendaProduto'
  ) THEN 'OK' ELSE 'NAO ENCONTRADA - vinculo de venda foi ignorado neste ajuste' END
UNION ALL
SELECT
  'Coluna CorteCor_VendaProduto.IdVendaProduto',
  CASE WHEN EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CorteCor_VendaProduto' AND COLUMN_NAME = 'IdVendaProduto'
  ) THEN 'OK' ELSE 'NAO ENCONTRADA - vinculo de venda por produto indisponivel' END;

SELECT
  'CorteCor_Pagamento' AS Tabela,
  COLUMN_NAME AS Coluna,
  IS_NULLABLE AS AceitaNull,
  COLUMN_TYPE AS Tipo,
  COLUMN_DEFAULT AS ValorPadrao
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'CorteCor_Pagamento'
  AND COLUMN_NAME IN ('IdSalao', 'IdAgendamento', 'IdPedido', 'IdVendaProduto', 'OrigemPagamento')
ORDER BY FIELD(COLUMN_NAME, 'IdSalao', 'IdAgendamento', 'IdPedido', 'IdVendaProduto', 'OrigemPagamento');
