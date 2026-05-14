## Plano de implantacao do modulo de vendas e estoque

### Etapa 1 - Persistencia

- criar tabela de venda
- criar tabela de itens da venda
- criar tabela de movimentos de estoque
- criar indices basicos para consulta

### Etapa 2 - Dominio e servicos

- criar modelos de venda, item de venda e movimento de estoque
- criar handler de venda e estoque
- criar servico de venda para validacao, calculo e finalizacao
- criar servico fiscal da venda para emissao apenas de servicos

### Etapa 3 - Integracoes

- integrar venda ao financeiro como titulo a receber
- integrar venda ao estoque com baixa e estorno
- integrar venda ao fiscal compartilhado
- permitir consulta da nota fiscal por venda

### Etapa 4 - Telas

- tela de vendas com checkout
- tela de historico de vendas
- tela de estoque com posicao, alertas e ajustes
- links de menu para acesso direto

### Etapa 5 - Validacao

- compilar e corrigir erros
- criar testes de servico
- validar fluxo em tela
- emitir nota fiscal apenas em homologacao e apenas para venda de servico
