# Plano de Implantacao do CRM

## Arquitetura adotada

O CRM foi implantado como um modulo transversal sobre a base existente:

- `Pessoa` continua sendo a fonte principal de clientes.
- `CrmPessoaPerfil` concentra preferencias e estado do relacionamento.
- `CrmInteracao` concentra contatos manuais e automaticos.
- `CrmTarefa` concentra follow-ups.
- `CrmEtapaFunil` e `CrmOportunidade` concentram pipeline.
- `CrmCampanha` e `CrmCampanhaDestino` concentram campanhas e resultados.

## Regras principais

- o cadastro atual de clientes nao foi quebrado
- o fluxo de enviar credenciais nao foi alterado
- o CRM reaproveita os fornecedores existentes de e-mail, SMS e WhatsApp
- o historico do cliente mistura CRM e eventos operacionais do sistema
- etapas do funil sao auto-semeadas por salao quando o CRM e acessado

## Telas implantadas

- `/CRM/Index`
- `/CRM/Cliente`
- `/CRM/Tarefas`
- `/CRM/Oportunidades`
- `/CRM/Campanhas`

## Integracoes

- lista de clientes ganhou atalho para o perfil CRM
- menu principal ganhou secao CRM
- timeline do cliente le:
  - agendamentos
  - pagamentos
  - notas fiscais
  - envios de lembretes
  - interacoes CRM
  - tarefas CRM
  - oportunidades
  - campanhas

## Limites conhecidos

- campanhas automaticas funcionam para e-mail, SMS e WhatsApp
- o canal WhatsApp foi entregue com suporte automatico para fornecedores compativeis com `Z-API` e `Evolution API`
