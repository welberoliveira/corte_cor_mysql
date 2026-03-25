# Levantamento do Modulo Financeiro e Relatorios de CRM

Data da analise: 2026-03-23

## Referencias de mercado consultadas

Financeiro:

- [Conta Azul - Gestao Financeira](https://contaazul.com/funcionalidades/gestao-financeira/)
- [Conta Azul - Mais relatorios financeiros](https://ajuda.contaazul.com/hc/pt-br/articles/27092209971469-Mais-relat%C3%B3rios-financeiros)
- [Omie - Controle Financeiro Empresarial](https://www.omie.com.br/funcionalidades/controle-financeiro-empresarial/)
- [Omie - Checklist do modulo de financas](https://ajuda.omie.com.br/pt-BR/articles/499120-checklist-do-modulo-de-financas)
- [Nibo - Gerenciador Financeiro](https://www.nibo.com.br/contador/funcionalidades/gerenciador-financeiro)

CRM e analytics:

- [HubSpot - Dashboard and Reporting Software](https://www.hubspot.com/products/reporting-dashboards)
- [HubSpot - Sales Reporting and Performance](https://www.hubspot.com/products/sales/sales-reports)
- [Pipedrive - Sales CRM reports and insights](https://www.pipedrive.com/en/features/insights-and-reports)
- [Salesforce - CRM Analytics](https://www.salesforce.com/analytics/crm/)

## Como os melhores modulos financeiros costumam funcionar

O padrao recorrente entre Conta Azul, Omie e Nibo converge nos mesmos blocos:

- contas a pagar e contas a receber
- fluxo de caixa realizado e projetado
- conciliacao bancaria e conciliacao de caixa
- plano de contas e categorizacao financeira
- contas correntes, caixas e carteiras
- cobranca, boletos e lembretes de inadimplencia
- DRE e relatorios gerenciais
- indicadores de vencidos, liquidados e saldo futuro
- cruzamento com cliente, fornecedor, forma de pagamento e centro de custo
- visoes analiticas com filtros, exportacao e graficos

Inferencia:
os produtos mudam a embalagem, mas a estrutura conceitual e muito parecida. O modulo financeiro completo gira em torno de titulos, contas, classificacao contabil-gerencial, caixa e relatorios.

## O que o sistema ja tinha antes desta implantacao

Antes desta entrega, o sistema ja possuia:

- cadastros de meios de pagamento
- cadastro e listagem de pagamentos
- plano de contas e contas caixa basicos
- um resumo antigo de margem de produtos e servicos em `FinanceiroResumo`
- dados de pagamentos de agendamentos

Mas ainda faltavam blocos essenciais para um modulo financeiro de verdade:

- titulo financeiro unificado para receber e pagar
- dashboard financeiro dedicado
- contas a receber derivadas dos pagamentos do negocio
- conta a pagar manual
- aging de inadimplencia
- fluxo de caixa projetado
- DRE gerencial consolidada
- relatorios por plano, forma de recebimento e cliente
- acoes de liquidar, reabrir, cancelar e conciliar
- filtros e visao operacional de lancamentos

## Gap identificado no sistema

O sistema estava forte em operacao, mas fraco em controladoria e analise.

Faltava:

- camada propria de `FinanceiroTitulo`
- sincronizacao entre pagamento operacional e titulo financeiro
- visao de carteira aberta e vencida
- diferenciação clara entre valor original, liquidado e aberto
- modulo de relatorios financeiros com graficos
- visao de concentracao de receita e despesa
- ranking de clientes por recebimento
- indicadores de saldo operacional e saldo projetado

## Requisitos de relatorios financeiros considerados obrigatorios

Os relatorios e graficos que um usuario costuma esperar, e que foram priorizados nesta implantacao, sao:

- dashboard com KPIs
- fluxo de caixa diario
- fluxo projetado
- DRE gerencial
- contas a receber em aberto
- contas a pagar em aberto
- titulos vencidos
- inadimplencia por faixa
- receitas por forma de pagamento
- receitas por plano de contas
- despesas por plano de contas
- top clientes por recebimento
- lista operacional de titulos com filtros

## Requisitos de relatorios de CRM considerados obrigatorios

Com base em HubSpot, Pipedrive e Salesforce, os blocos mais importantes para CRM analitico sao:

- clientes por status de relacionamento
- clientes por temperatura
- interacoes por canal
- tarefas por status
- oportunidades por etapa do funil
- valor do pipeline aberto
- campanhas por canal
- clientes em risco
- proximas acoes
- ultimas campanhas

## Escopo implantado nesta entrega

Financeiro:

- dashboard financeiro
- lancamentos financeiros
- plano de contas
- contas caixa
- relatorios financeiros
- sincronizacao de pagamentos operacionais com contas a receber
- liquidacao, reabertura, cancelamento e conciliacao

CRM:

- relatorios de CRM dedicados
- graficos de status, canal e funil
- tabelas de clientes em risco, proximas acoes, tarefas e campanhas

## Graficos e relatorios implementados

Financeiro:

- grafico de fluxo de caixa
- grafico de receitas por forma
- grafico de despesas por plano
- grafico de fluxo projetado
- grafico de inadimplencia por faixa
- tabela de DRE
- tabela de titulos criticos
- tabela de titulos do periodo
- tabela de top clientes

CRM:

- grafico de clientes por status
- grafico de interacoes por canal
- grafico de valor do funil por etapa
- tabela de clientes em risco
- tabela de proximas acoes
- tabela de tarefas por status
- tabela de ultimas campanhas

## Arquivos principais da implantacao

- `Pages/Financeiro/Index.cshtml`
- `Pages/Financeiro/Lancamentos.cshtml`
- `Pages/Financeiro/Relatorios.cshtml`
- `Pages/Financeiro/PlanoContas.cshtml`
- `Pages/Financeiro/ContasCaixa.cshtml`
- `Pages/CRM/Relatorios.cshtml`
- `Services/FinanceiroService.cs`
- `Handlers/FinanceiroModuloHandler.cs`
- `Models/FinanceiroModels.cs`
- `Models/CrmRelatorioModels.cs`
- `Sql/20260323_modulo_financeiro_relatorios.sql`
