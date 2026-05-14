# Checklist de Revisao da Migracao SQL Server -> MySQL

## Checklist executado

- [x] Revisar a configuracao principal da aplicacao para carregar `DefaultConnection` em MySQL e priorizar segredos fora de arquivos versionados.
- [x] Remover segredos reais de `appsettings*.json` rastreados e mover configuracoes sensiveis para `appsettings.Local.json`.
- [x] Endurecer autenticacao com hash PBKDF2 para novas senhas e compatibilidade com hashes legados.
- [x] Revisar envio de credenciais e fluxos de senha para evitar dependencia de senha em texto puro/Base64.
- [x] Remover dependencia do provider SQL Server do projeto principal.
- [x] Revisar a camada de conexao para MySQL com validacao de connection string e tratamento operacional para limite de conexoes da hospedagem.
- [x] Reduzir round-trips nas telas de venda e pedido com carga combinada de catalogos em uma unica consulta.
- [x] Adicionar cache em validacoes/estruturas base de CRM e Financeiro para aliviar leituras repetitivas no MySQL.
- [x] Adicionar indices MySQL nas tabelas mais quentes do schema para melhorar filtros, joins e listagens.
- [x] Corrigir incompatibilidades de SQL Server remanescentes encontradas em tempo de execucao durante a abertura das telas.
- [x] Corrigir o carregamento da tela `FuncionarioServicoLista`, incluindo a coluna `IdCategoria` ausente na consulta.
- [x] Corrigir mensagens com encoding quebrado nas validacoes fiscais e de agendamento usadas por tela e por testes.
- [x] Alinhar a suite de testes com as novas dependencias de cache em `CrmService` e `FinanceiroService`.
- [x] Executar `dotnet build` da solucao com sucesso.
- [x] Executar `dotnet test` do projeto `CorteCor.Tests` com 235/235 testes aprovados.
- [x] Validar abertura autenticada das telas criticas do usuario por HTTP direto contra a aplicacao rodando em MySQL.
- [x] Validar abertura autenticada das telas administrativas principais por HTTP direto no fluxo `/Adm`.

## Telas validadas

- [x] `/Dashboard`
- [x] `/Dashboard?handler=Data`
- [x] `/Agendamentos`
- [x] `/CRM`
- [x] `/CRM/Tarefas`
- [x] `/CRM/Oportunidades`
- [x] `/Financeiro`
- [x] `/Financeiro/PlanoContas`
- [x] `/Financeiro/ContasCaixa`
- [x] `/Pedidos`
- [x] `/Pedidos/Novo`
- [x] `/Vendas`
- [x] `/Vendas/Novo`
- [x] `/Fiscal/NotaFiscalAvulsa`
- [x] `/FuncionarioServicoLista`
- [x] `/ServicoLista`
- [x] `/ProdutoLista`
- [x] `/PessoaLista`
- [x] `/Painel`
- [x] `/SistemaLogLista`
- [x] `/UsuarioLista`

## Escopo removido

- [x] Tela e recursos de `Estabelecimentos` removidos do escopo da migracao MySQL.

## Melhorias recomendadas apos esta revisao

- [ ] Reduzir os warnings de nulabilidade e `using` duplicado para aumentar seguranca de refatoracao.
- [ ] Padronizar encoding/acentuacao em outros arquivos que ainda exibem texto mojibake fora dos fluxos corrigidos nesta rodada.
- [ ] Adicionar health checks para MySQL e servicos externos criticos.
- [ ] Medir tempos de resposta com logs/metricas por rota para localizar gargalos reais alem do limite `max_user_connections` da hospedagem compartilhada.
- [ ] Avaliar aumento de capacidade do MySQL hospedado ou pool de conexoes mais permissivo se a carga de uso crescer.
