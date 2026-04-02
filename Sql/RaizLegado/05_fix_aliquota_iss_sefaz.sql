-- A Sefaz Nacional rejeita a emissão de NFS-e (Erro E0595) caso a alíquota informada
-- seja superior ao limite legal de 5%.
-- Este script garante que nenhum serviço configurado no sistema ultrapasse esse limite.

BEGIN TRANSACTION;

UPDATE CorteCor_Servico
SET AliquotaISS = 5.0
WHERE AliquotaISS > 5.0;

COMMIT;
