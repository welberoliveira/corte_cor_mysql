# Preparacao da Fase 2 - Agendamentos e Vendas

## Objetivo

Padronizar a integracao futura de `Agendamentos` e `Vendas` com o nucleo fiscal consolidado na `Nota Fiscal Avulsa`.

## Contrato adotado

- Origem fiscal padronizada em `FiscalOrigemRequest`
- Cliente padronizado em `FiscalOrigemCliente`
- Itens fiscais padronizados em `FiscalOrigemItem`
- Envelope de integracao padronizado em `FiscalOrigemEnvelope`

Arquivos:

- `C:\Welber\2022\GitHubDesktop\corte_cor_ag\Services\FiscalOrigemModels.cs`
- `C:\Welber\2022\GitHubDesktop\corte_cor_ag\Services\FiscalOrigemPreparationService.cs`

## Regra para Agendamentos

- `Agendamentos` devem mapear cliente e servicos para `FiscalOrigemEnvelope`
- a emissao continua passando pelo mesmo nucleo da tela avulsa
- a numeracao, XML, PDF, e-mail, cancelamento e historico continuam centralizados no modulo fiscal

## Regra para Vendas

- `Vendas` devem mapear cliente e itens para `FiscalOrigemEnvelope`
- produtos e servicos devem sair da origem normalizados, sem regra fiscal espalhada pela UI de venda
- a decisao final do tipo de documento continua centralizada no nucleo fiscal

## Proximo passo tecnico

1. Ler a origem (`Agendamento` ou `Venda`)
2. Montar `FiscalOrigemEnvelope`
3. Converter o envelope para `NotaFiscalAvulsaRequest`
4. Reaproveitar o mesmo fluxo homologado na tela avulsa

## Ganho esperado

- menos duplicacao de regra fiscal
- menos divergencia entre tela avulsa, agendamento e venda
- mesma trilha de auditoria e homologacao para todas as origens
