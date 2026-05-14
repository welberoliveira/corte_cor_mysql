# Checklist de Homologacao - Fase 1 Nota Fiscal Avulsa

## Objetivo

Validar a tela de Nota Fiscal Avulsa como nucleo fiscal confiavel antes da expansao para Agendamentos e Vendas.

## Preparacao

- [ ] Confirmar configuracao fiscal do salao preenchida
- [ ] Confirmar certificado digital valido e senha correta
- [ ] Confirmar ambiente selecionado corretamente: homologacao ou producao
- [ ] Confirmar numeracao e serie esperadas para o tipo de nota
- [ ] Confirmar fornecedor de e-mail configurado para testes

## Emissao NFS-e

- [ ] Emitir NFS-e avulsa com sucesso
- [ ] Validar persistencia da nota na base
- [ ] Validar XML de envio disponivel
- [ ] Validar XML de retorno disponivel
- [ ] Validar protocolo salvo
- [ ] Validar chave nacional salva
- [ ] Validar PDF gerado
- [ ] Validar resumo tecnico do retorno exibido na tela

## Emissao NF-e ou NFC-e

- [ ] Emitir NF-e avulsa com sucesso
- [ ] Emitir NFC-e avulsa com sucesso
- [ ] Validar persistencia de protocolo, chave e XMLs
- [ ] Validar PDF gerado
- [ ] Validar consulta posterior da nota no historico local

## Rejeicao e falha controlada

- [ ] Forcar rejeicao por dado fiscal invalido
- [ ] Confirmar status rejeitado
- [ ] Confirmar justificativa retornada pelo provedor fiscal
- [ ] Confirmar registro de log tecnico
- [ ] Confirmar que a tela continua operacional apos a falha

## Consulta de status

- [ ] Consultar uma NFS-e ja emitida
- [ ] Consultar uma NF-e ja emitida
- [ ] Consultar uma NFC-e ja emitida
- [ ] Confirmar que o tipo real da nota foi respeitado na consulta
- [ ] Confirmar atualizacao de status, protocolo e XML de retorno

## Cancelamento

- [ ] Cancelar uma nota autorizada com justificativa valida
- [ ] Confirmar persistencia do evento de cancelamento
- [ ] Confirmar status atualizado para cancelada
- [ ] Confirmar historico da nota atualizado na tela

## Carta de correcao

- [ ] Enviar CC-e para NF-e autorizada
- [ ] Enviar CC-e para NFC-e autorizada
- [ ] Confirmar persistencia do evento
- [ ] Confirmar exibicao do evento no historico local

## Inutilizacao

- [ ] Inutilizar faixa de numeracao valida
- [ ] Confirmar persistencia da inutilizacao
- [ ] Confirmar protocolo e XML de retorno
- [ ] Confirmar mensagem de retorno exibida na tela

## E-mail fiscal

- [ ] Enviar nota por e-mail a partir do resultado da transmissao
- [ ] Enviar nota por e-mail a partir do historico local
- [ ] Confirmar anexo de PDF
- [ ] Confirmar anexo de XML de envio quando disponivel
- [ ] Confirmar anexo de XML de retorno quando disponivel
- [ ] Confirmar log de envio registrado

## Historico local

- [ ] Abrir historico detalhado de uma nota
- [ ] Confirmar ordenacao decrescente de eventos
- [ ] Confirmar ordenacao decrescente de logs
- [ ] Confirmar exibicao de status, codigo, protocolo e mensagem tecnica

## Fechamento

- [ ] Validar todos os testes automatizados direcionados da fase 1
- [ ] Registrar evidencias de homologacao por tipo de nota
- [ ] Aprovar a tela avulsa como base para reutilizacao
