# Plano de Implantacao do Modulo Financeiro e Relatorios de CRM

## Objetivo

Transformar o sistema em uma plataforma com:

- operacao financeira diaria
- visao gerencial de resultados
- relatorios financeiros comparaveis aos modulos centrais de Conta Azul, Omie e Nibo
- relatorios de CRM adequados para analise de relacionamento, funil e campanhas

## Estrategia adotada

### Fase 1 - Base de dados e modelos

- criar entidade unificada de titulo financeiro
- criar estrutura de plano de contas e contas caixa quando inexistentes
- permitir vinculo com cliente, agendamento e pagamento

### Fase 2 - Servicos e sincronizacao

- criar handler proprio do modulo financeiro
- sincronizar pagamentos operacionais com titulos a receber
- consolidar regras de status, liquidacao, cancelamento e conciliacao

### Fase 3 - Operacao web

- dashboard financeiro
- tela de lancamentos
- tela de plano de contas
- tela de contas caixa
- tela de relatorios financeiros
- tela de relatorios de CRM

### Fase 4 - Analytics

- KPIs principais
- fluxo de caixa realizado
- fluxo projetado
- DRE gerencial
- inadimplencia por faixa
- ranking de clientes
- funil e atividades de CRM

### Fase 5 - Validacao

- build da aplicacao
- build do projeto de testes
- testes de servico e DI
- ajuste de erros de compilacao

## Regras funcionais implantadas

Financeiro:

- pagamentos do negocio alimentam contas a receber
- cada titulo possui tipo, origem, vencimento, valor original, valor liquidado e valor aberto
- titulos podem ser `Aberto`, `Vencido`, `Liquidado` ou `Cancelado`
- contas, planos e relatorios sao filtrados por `IdSalao`

CRM:

- relatorios leem o CRM ja implantado e consolidam relacao com clientes, tarefas, oportunidades e campanhas
- visoes analiticas sao filtradas por periodo e `IdSalao`

## Critios de aceite adotados

- aplicacao compila sem erros
- projeto de testes compila sem erros
- testes direcionados do financeiro, CRM e DI passam
- telas novas renderizam no projeto
- script unico de banco fica pronto para aplicacao

## Expansoes futuras recomendadas

- conciliacao bancaria automatica por extrato importado
- centros de custo
- recorrencia de titulos
- aprovacao de contas a pagar
- previsao por regime de competencia x caixa
- exportacao em Excel/PDF dos relatorios
- metas financeiras e comparativo orcado x realizado
- cohort de CRM, churn e LTV
