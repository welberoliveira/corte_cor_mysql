-- Inserir notas NFS-e já emitidas em ambiente de homologação para evitar duplicidade
-- IdSalao assumido como o da Tonni Tecnologia (precisa confirmar se é o logado)
-- Ambiente 2 = Homologação

DECLARE @IdSalao INT;
SELECT TOP 1 @IdSalao = IdSalao FROM CorteCor_Salao WHERE Cnpj = '49358717000107';

IF @IdSalao IS NOT NULL
BEGIN
    -- NFS-e Número 1
    IF NOT EXISTS (SELECT 1 FROM CorteCor_NotaFiscal WHERE Numero = 1 AND Serie = 70000 AND TipoNota = 'NFS-e' AND Ambiente = 2 AND IdSalao = @IdSalao)
    BEGIN
        INSERT INTO CorteCor_NotaFiscal (IdNotaFiscal, IdSalao, TipoNota, Ambiente, Numero, Serie, ValorTotal, Status, ChaveAcessoNacional, DataEmissao, DataAtualizacao)
        VALUES (NEWID(), @IdSalao, 'NFS-e', 2, 1, 70000, 10.00, 'Autorizada', 'NFS31433022249358717000107000000000000126032097898739', '2026-03-11 16:17:15', GETDATE());
    END

    -- NFS-e Número 2
    IF NOT EXISTS (SELECT 1 FROM CorteCor_NotaFiscal WHERE Numero = 1 AND Serie = 1 AND TipoNota = 'NFS-e' AND Ambiente = 2 AND IdSalao = @IdSalao)
    BEGIN
        INSERT INTO CorteCor_NotaFiscal (IdNotaFiscal, IdSalao, TipoNota, Ambiente, Numero, Serie, ValorTotal, Status, ChaveAcessoNacional, DataEmissao, DataAtualizacao)
        VALUES (NEWID(), @IdSalao, 'NFS-e', 2, 1, 1, 10.00, 'Autorizada', 'NFS31433022249358717000107000000000000226038481475256', '2026-03-11 18:17:40', GETDATE());
    END
END
GO
