# Checklist da Preparacao para a Fase 2

## Antes da execucao

- [ ] Proteger as paginas de agendamento com `UsuarioPolicy`
- [ ] Padronizar os status de agendamento e remover regras espalhadas de `"Confirmado" -> "Pago"`
- [ ] Extrair a logica critica de agenda e disponibilidade para um servico dedicado
- [ ] Substituir a emissao fiscal legada de `Agendamentos2` por uma ponte para o nucleo fiscal homologado
- [ ] Remover a instanciação manual critica de handlers em `PagamentoCadastro` e alinhar o gatilho fiscal automatico
- [ ] Cobrir a preparacao com testes e validacao de build

## Depois da execucao

- [x] Proteger as paginas de agendamento com `UsuarioPolicy`
- [x] Padronizar os status de agendamento e remover regras espalhadas de `"Confirmado" -> "Pago"`
- [x] Extrair a logica critica de agenda e disponibilidade para um servico dedicado
- [x] Substituir a emissao fiscal legada de `Agendamentos2` por uma ponte para o nucleo fiscal homologado
- [x] Remover a instanciação manual critica de handlers em `PagamentoCadastro` e alinhar o gatilho fiscal automatico
- [x] Cobrir a preparacao com testes e validacao de build

## Resultado da preparacao

- O calendario e os handlers web de agendamento passaram a usar um servico dedicado para disponibilidade, eventos e consistencia de horario.
- O fluxo fiscal do agendamento deixou de montar nota por logica propria e passou a preparar uma origem fiscal para o nucleo homologado da nota avulsa.
- O pagamento manual deixou de usar `new Handler()` no fluxo principal e agora pode disparar emissao automatica pelo mesmo nucleo fiscal do agendamento.
- Os status de agendamento ficaram centralizados e consistentes para exibicao, pagamento e emissao.
