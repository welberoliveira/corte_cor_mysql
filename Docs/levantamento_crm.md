# Levantamento de CRM no CorteCor

## O que o sistema ja tinha antes do CRM

- Base de clientes e contatos em `Pessoa`, com endereco, documento, tags, observacoes e datas comemorativas.
- Historico operacional de agendamentos, pagamentos e notas fiscais vinculados ao cliente.
- Modelos de comunicacao por e-mail e SMS.
- Fornecedores configuraveis de e-mail, SMS e WhatsApp.
- Lembretes automaticos de agendamento com log de envios.

## Recursos tipicos de CRM digital encontrados em plataformas de mercado

Com base em materiais oficiais de CRM, os blocos mais comuns sao:

- gestao de contatos e visao unica do cliente
- historico de interacoes multicanal
- funil e pipeline com oportunidades
- tarefas e follow-ups
- campanhas segmentadas
- automacoes e lembretes
- dashboard com indicadores de relacionamento

Referencias usadas:

- [HubSpot Sales Hub](https://www.hubspot.com/hubfs/assets/flywheel%20campaigns/Improving%20Productivity%20Per%20Rep%20with%20Sales%20Hub%20Enterprise.pdf?hubs_content=offers.hubspot.com%2Fsales-hubs-latest-and-greatest-ty&hubs_content-cta=pdf)
- [Salesforce Lead Management Guide](https://resources.docs.salesforce.com/latest/latest/en-us/sfdc/pdf/salesforce_lead_implementation_guide.pdf)
- [Zoho CRM multichannel CRM](https://www.zoho.com/crm/images/multi-channel-crm.pdf)

Inferencia:
essas plataformas usam nomenclaturas diferentes, mas convergem nos mesmos pilares de CRM. O plano adotado no CorteCor foi baseado nessa convergencia.

## Gap identificado no sistema

O sistema ja possuia comunicacao operacional e cadastro de clientes, mas ainda nao tinha um CRM completo porque faltavam:

- dashboard dedicado de relacionamento
- perfil 360 do cliente
- timeline unica consolidando eventos do cliente
- tarefas de follow-up
- funil com oportunidades
- campanhas segmentadas usando a base de clientes
- preferencias de contato centralizadas por cliente

## Escopo adotado para o CRM implantado

Para encaixar no sistema atual sem quebrar fluxos existentes, o CRM foi modelado como uma camada propria, integrada a:

- clientes (`Pessoa`)
- agendamentos
- pagamentos
- fiscal
- lembretes
- envios digitais

Os modulos implantados foram:

- Dashboard CRM
- Cliente 360
- Tarefas CRM
- Oportunidades CRM
- Campanhas CRM por e-mail, SMS e WhatsApp
