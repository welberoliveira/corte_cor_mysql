# Checklist de Execucao da Fase 2 - Agendamentos -> Fiscal

## Antes da execucao

- [ ] Confirmar que o agendamento so emite nota quando estiver `Pago`
- [ ] Garantir que a fase 2 emita `NFS-e` pelo nucleo fiscal compartilhado
- [ ] Bloquear nota ativa duplicada por `IdAgendamento`
- [ ] Permitir reemissao quando a ultima nota do agendamento estiver `Rejeitada` ou `Cancelada`
- [ ] Registrar se a emissao foi manual ou automatica
- [ ] Refletir a situacao fiscal no modal de `Agendamentos2`
- [ ] Refletir a situacao fiscal em `AgendamentosLista`
- [ ] Abrir a nota vinculada a partir do agendamento
- [ ] Filtrar `NotaFiscalLista` por agendamento
- [ ] Validar o fluxo manual em homologacao
- [ ] Validar o fluxo automatico em homologacao
- [ ] Validar build e testes automatizados da fase 2

## Depois da execucao

- [x] Confirmar que o agendamento so emite nota quando estiver `Pago`
- [x] Garantir que a fase 2 emita `NFS-e` pelo nucleo fiscal compartilhado
- [x] Bloquear nota ativa duplicada por `IdAgendamento`
- [x] Permitir reemissao quando a ultima nota do agendamento estiver `Rejeitada` ou `Cancelada`
- [x] Registrar se a emissao foi manual ou automatica
- [x] Refletir a situacao fiscal no modal de `Agendamentos2`
- [x] Refletir a situacao fiscal em `AgendamentosLista`
- [x] Abrir a nota vinculada a partir do agendamento
- [x] Filtrar `NotaFiscalLista` por agendamento
- [x] Validar o fluxo manual em homologacao
- [x] Validar o fluxo automatico em homologacao
- [x] Validar build e testes automatizados da fase 2

## Evidencias principais

- Emissao manual homologada a partir do agendamento `#51` com `NFS-e 1060/1` autorizada.
- Emissao automatica homologada a partir do pagamento do agendamento `#52` com `NFS-e 1061/1` autorizada.
- `AgendamentosLista` passou a exibir badge fiscal e atalho para a nota vinculada.
- `NotaFiscalLista` passou a aceitar filtro por `idAgendamento`.
- O servico `Desenvolvimento de Sistema Simples` foi homologado com codigo de tributacao nacional valido para o fluxo de NFS-e.
