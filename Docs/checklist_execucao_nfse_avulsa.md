# Checklist de Execucao - NFS-e Avulsa

## Etapa 1 - Consolidacao do Checklist
- [x] Consolidar o estado atual do fluxo de NFS-e Avulsa em homologacao
- [x] Confirmar o que ja esta funcionando no browser
- [x] Confirmar as pendencias reais de desenvolvimento
- [x] Confirmar as pendencias reais de teste
Resultado da etapa: concluida.

## Etapa 2 - Correcoes Imediatas no Fluxo Avulso
- [x] Impedir que a consulta regredisse nota cancelada para rejeitada
- [x] Tratar cancelamento idempotente `E0840` como estado final cancelado
- [x] Sanitizar protocolo tecnico indevido no historico
- [x] Expor PDF tambem para nota cancelada no grid da tela avulsa
- [x] Revisar exibicao dos campos de retorno fiscal na UI
Resultado da etapa: concluida.

## Etapa 3 - Testes de Homologacao NFS-e
- [x] Emitir NFS-e autorizada em homologacao
- [x] Consultar NFS-e autorizada em homologacao
- [x] Baixar XML de NFS-e autorizada
- [x] Gerar PDF de NFS-e autorizada
- [x] Enviar NFS-e autorizada por e-mail
- [x] Cancelar NFS-e em homologacao
- [x] Consultar NFS-e cancelada sem regredir status
- [x] Verificar historico de NFS-e cancelada
- [x] Validar PDF pela UI para NFS-e cancelada
- [x] Validar fluxo com tomador PJ em homologacao
- [x] Validar novo codigo de tributacao/servico homologado
- [x] Validar persistencia apos reiniciar aplicacao
- [x] Validar concorrencia de numeracao em emissoes sequenciais
Resultado da etapa: concluida.

## Etapa 4 - Hardening e Expansao
- [x] Fechar contrato de consulta NFS-e por chave x DPS de forma explicita
- [x] Sanear dados legados afetados por bugs anteriores
- [x] Reduzir mais logica restante da PageModel
- [x] Fechar a preparacao para Agendamentos
- [x] Fechar a preparacao para Vendas
- [x] Limpar warnings tecnicos criticos ligados ao fluxo fiscal
Resultado da etapa: concluida.
