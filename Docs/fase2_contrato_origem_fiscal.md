# Contrato de Origem Fiscal

## Objetivo

Padronizar como o nucleo fiscal da `Nota Fiscal Avulsa` sera reaproveitado depois em `Agendamentos` e `Vendas`.

## Tipos de origem

- `Avulsa`
- `Agendamento`
- `Venda`

## Contrato minimo

- `Origem`
- `IdOrigem`
- `IdSalao`
- `ReferenciaExterna`

## Responsabilidade por origem

### Avulsa

- fornece os dados preenchidos diretamente na tela fiscal
- nao depende de outra entidade de negocio

### Agendamento

- fornece cliente, servicos, valores e referencia do agendamento
- nao deve conter regra fiscal propria fora do mapeamento de origem

### Venda

- fornece itens, tributos, cliente e referencia da venda
- nao deve emitir diretamente sem passar pelo nucleo fiscal comum

## Regra de arquitetura

- a emissao fiscal deve continuar centralizada no nucleo ja consolidado pela tela avulsa
- `Agendamentos` e `Vendas` devem apenas mapear sua origem para esse contrato
- o fluxo de status, logs, eventos, XML, PDF e e-mail deve ser o mesmo para qualquer origem
