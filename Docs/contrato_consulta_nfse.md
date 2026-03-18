# Contrato de Consulta NFS-e Nacional

## Objetivo

Explicitar qual identificador o sistema usa para consultar a NFS-e no Padrao Nacional e em que ordem isso acontece.

## Regra de consulta

1. A aplicacao tenta primeiro a consulta pela propria chave da NFS-e.
2. Se o retorno da consulta por chave vier com `E2406`, a aplicacao interpreta esse retorno como consulta inconclusiva para aquele identificador.
3. Nessa situacao, a aplicacao tenta novamente a consulta usando o `Id` da DPS extraido do XML de envio ou do XML de retorno da nota.

## Motivacao

- O ambiente nacional pode responder de forma diferente quando a consulta e feita com o identificador da NFS-e ou com o identificador da DPS.
- O fluxo de cancelamento e consulta nao deve regredir o status local da nota apenas porque a consulta por chave retornou um erro tecnico ou inconclusivo.

## Implementacao atual

- Consulta por chave: `ConsultarNfsePorChave`
- Consulta por DPS: `ConsultarNfsePorDps`
- Ponto de orquestracao: `ConsultarAsync`

Arquivo principal:

- `C:\Welber\2022\GitHubDesktop\corte_cor_ag\Services\NotaFiscalAvulsaService.cs`

## Regras de sincronizacao de status

- Antes de consultar o provedor, o sistema tenta sincronizar um eventual cancelamento ja conhecido localmente ou ja encontrado no provedor.
- Se o cancelamento ja estiver consolidado, a nota permanece `Cancelada`.
- Um erro tecnico de consulta nao deve sobrescrever um estado fiscal final ja consolidado.

## Evidencias homologadas

- Consulta de NFS-e autorizada: validada.
- Consulta de NFS-e cancelada: validada sem regredir para `Rejeitada`.
- Cancelamento idempotente `E0840`: tratado como `Cancelada`.
