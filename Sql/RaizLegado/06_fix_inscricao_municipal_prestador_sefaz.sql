-- A Sefaz Nacional rejeita a emissão de NFS-e (Erro E0116) se a Inscrição Municipal (IM)
-- do Prestador de Serviços não for enviada no XML (tag <IM> dentro de <Prest>).
-- Este script garante que o campo InscricaoMunicipal esteja correto.

BEGIN TRANSACTION;

-- Substitua 'SEU_NUMERO_DE_IM_AQUI' pela Inscrição Municipal real do salão
-- Caso seja para homologação, mantenha a IM real do emissor, pois a Sefaz valida
-- contra o Cadastro Nacional de Contribuintes (CNC).

UPDATE CorteCor_ConfigFiscal
SET InscricaoMunicipal = '123456' -- <-- COLOQUE A INSCRIÇÃO MUNICIPAL CORRETA AQUI
WHERE InscricaoMunicipal IS NULL OR InscricaoMunicipal = '';

COMMIT;
