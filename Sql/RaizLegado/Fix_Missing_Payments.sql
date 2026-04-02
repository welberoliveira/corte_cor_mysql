-- Script para criar registros de pagamento para agendamentos que já estão como "Pago" ou "Confirmado"
-- mas que, por algum motivo do legado, não geraram registros na tabela CorteCor_Pagamento.

BEGIN TRANSACTION;

INSERT INTO CorteCor_Pagamento (
    IdPagamento, 
    IdAgendamento, 
    Ativo, 
    Status, 
    Valor, 
    Moeda, 
    Descricao, 
    Tipo
    -- Removidos CriadoEm e PagoEm caso o banco tenha triggers ou DEFAULT constraints para eles,
    -- e para parear com a query no PagamentoHandler
)
SELECT 
    NEWID() AS IdPagamento,
    a.IdAgendamento,
    1 AS Ativo,
    'Pago' AS Status,
    ISNULL(s.Preco, 0) AS Valor,
    'BRL' AS Moeda,
    'Pagamento do agendamento ' + CAST(a.IdAgendamento AS VARCHAR(50)) + ' (Gerado a partir do Status Antigo)' AS Descricao,
    'Manual' AS Tipo
FROM 
    CorteCor_Agendamento a
INNER JOIN 
    CorteCor_Servico s ON a.IdServico = s.IdServico
LEFT JOIN 
    CorteCor_Pagamento p ON a.IdAgendamento = p.IdAgendamento
WHERE 
    a.Status IN ('Pago', 'Confirmado')
    AND p.IdPagamento IS NULL
    AND (a.Excluido = 0 OR a.Excluido IS NULL);

-- Caso a tabela possua colunas obrigatórias como CriadoEm e Data, o SQL Server pode apontar erro se não houver DEFAULT.
-- Se houver erro, basta adicionar GETUTCDATE() para CriadoEm e AtualizadoEm (dependendo da sua estrutura de banco).

COMMIT TRANSACTION;
