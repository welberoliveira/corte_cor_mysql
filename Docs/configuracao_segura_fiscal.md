# Configuracao Segura do Modulo Fiscal

## Objetivo

Este documento define o minimo necessario para executar o modulo fiscal sem manter segredos versionados no repositorio.

## Itens obrigatorios

- `ConnectionStrings__DefaultConnection`
- `FiscalSettings__MasterKey`
- `MercadoPago__AccessToken`
- `MercadoPago__PublicKey`

## Regras

- A `FiscalSettings__MasterKey` deve ter exatamente `32` caracteres.
- A chave mestra nao deve ser compartilhada entre ambientes.
- Producao e homologacao devem usar conexoes e segredos diferentes.
- Certificados A1 devem permanecer no banco apenas criptografados.
- A senha do certificado nao deve ser logada, exibida em tela ou hardcoded.

## Recomendacao operacional

- Em desenvolvimento local, prefira variaveis de ambiente ou um `appsettings.Development.json` fora do versionamento.
- Em homologacao e producao, prefira secret manager do servidor, pipeline ou painel seguro.
- Sempre rotacionar a chave mestra se houver suspeita de exposicao.

## Impacto desta fase

O `appsettings.json` do projeto agora contem apenas placeholders. Antes de subir a aplicacao em ambiente real, os valores devem ser preenchidos por configuracao segura.
