# Checklist de Implantacao do CRM

## Checklist inicial

- [x] Levantar os recursos digitais de relacionamento ja existentes no sistema
- [x] Comparar o sistema com capacidades tipicas de CRM digital
- [x] Definir a arquitetura do CRM sem quebrar clientes, agenda e comunicacao ja existentes
- [x] Criar persistencia propria para perfil, interacoes, tarefas, oportunidades e campanhas
- [x] Criar servico de negocio do CRM
- [x] Criar dashboard CRM
- [x] Criar pagina de cliente 360
- [x] Criar gestao de tarefas de follow-up
- [x] Criar gestao de oportunidades em funil
- [x] Criar gestao de campanhas segmentadas
- [x] Integrar CRM a lista de clientes
- [x] Integrar CRM ao menu principal
- [x] Criar script unico de banco para o CRM
- [x] Compilar a aplicacao
- [x] Compilar o projeto de testes
- [x] Criar testes do servico de CRM
- [x] Habilitar disparo automatico de campanhas por WhatsApp

## Entregaveis implantados

- [x] `Models/CrmModels.cs`
- [x] `Handlers/ICrmHandler.cs`
- [x] `Handlers/CrmHandler.cs`
- [x] `Services/CrmService.cs`
- [x] `Services/IWhatsappService.cs`
- [x] `Services/WhatsappService.cs`
- [x] `Pages/CRM/Index`
- [x] `Pages/CRM/Cliente`
- [x] `Pages/CRM/Tarefas`
- [x] `Pages/CRM/Oportunidades`
- [x] `Pages/CRM/Campanhas`
- [x] `Sql/20260323_crm_completo.sql`
- [x] `CorteCor.Tests/CrmServiceTests.cs`
- [x] `CorteCor.Tests/WhatsappServiceTests.cs`

## Fechamento final

- [x] Campanhas automaticas por e-mail
- [x] Campanhas automaticas por SMS
- [x] Campanhas automaticas por WhatsApp

Observacao:
o envio automatico por WhatsApp ficou pronto para fornecedores compativeis com `Z-API` e `Evolution API`, reutilizando o cadastro de fornecedores ja existente no sistema.
