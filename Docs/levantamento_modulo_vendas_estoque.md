## Levantamento do modulo de vendas e estoque

### Como modulos digitais maduros costumam funcionar

Com base em referencias oficiais de mercado, os modulos de venda e estoque mais usados combinam:

- checkout unico para itens de produto e servico
- baixa automatica de estoque na finalizacao da venda
- historico de movimentos de estoque para entrada, saida, ajuste e estorno
- venda vinculada a cliente, forma de pagamento e operador
- integracao com financeiro para contas a receber
- relatorios operacionais e alertas de estoque baixo
- integracao fiscal ao final da venda quando aplicavel

### Referencias usadas

- Shopify POS: foco em estoque, pedidos de compra, transferencias e operacao omnichannel
  - https://www.shopify.com/pos
- Square POS: foco em checkout, catalogo, estoque e relatorios operacionais
  - https://squareup.com/us/en/point-of-sale
- Lightspeed Retail: foco em estoque, relatorios e operacao de varejo
  - https://www.lightspeedhq.com/pos/retail/
- Omie ERP: foco em vendas, estoque e emissao de nota fiscal integrados
  - https://www.omie.com.br/erp/

### O que o sistema ja possui

- cadastro de produtos com preco, estoque atual, estoque minimo e controle de estoque
- cadastro de servicos funcionando bem pelo fluxo de agendamento
- modulo financeiro com contas a pagar e receber
- nucleo fiscal compartilhado e origem fiscal preparada para venda
- lista de notas fiscais capaz de relacionar `IdVendaProduto`

### O que ainda faltava

- entidade operacional de venda
- itens da venda com mistura de produtos e servicos
- movimento de estoque com historico e trilha auditavel
- tela de checkout de venda
- tela de historico de vendas
- tela de controle e ajustes de estoque
- integracao da venda com financeiro
- integracao da venda com emissao fiscal de servico em homologacao
- filtros e relatorios basicos de venda e estoque

### Restricao funcional importante

- servicos vendidos por agendamento devem continuar como estao
- emissao fiscal no modulo de vendas sera usada somente para servicos
- testes de emissao fiscal devem ocorrer sempre em homologacao
