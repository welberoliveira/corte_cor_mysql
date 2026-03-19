# Checklist de Execucao - Melhorias de Cadastros

Escopo executado:
- sem alterar o fluxo de `Enviar Credenciais`
- com `soft delete` para categorias de produto
- sem adicionar cor ao calendario de funcionarios
- sem criar novas colunas no banco para esses cadastros

## Clientes
- [x] Validacao forte de nome, e-mail, telefone e CPF/CNPJ no cadastro
- [x] Bloqueio de duplicidade por CPF/CNPJ no mesmo salao
- [x] Bloqueio de duplicidade por e-mail no mesmo salao
- [x] Resumo de validacao no formulario
- [x] Acao para copiar endereco principal para endereco de entrega
- [x] Lista com busca por nome, CPF/CNPJ, telefone e e-mail
- [x] Paginacao server-side da lista
- [x] Badges de tipo de contato na lista

## Funcionarios
- [x] Leitura por funcionario e salao no cadastro
- [x] Validacao de ao menos um dia de atendimento
- [x] Validacao de inicio e fim obrigatorios quando o dia esta ativo
- [x] Validacao de inicio menor que fim
- [x] Botao para copiar horario de segunda para dias uteis
- [x] Resumo de validacao no formulario
- [x] Lista com busca por nome
- [x] Paginacao server-side da lista
- [x] Sem adicao de cor no calendario

## Servicos
- [x] Validacao de nome obrigatorio
- [x] Bloqueio de duplicidade de nome por salao
- [x] Validacao de preco e preco de custo nao negativos
- [x] Validacao de duracao maior que zero
- [x] Validacao de aliquota ISS entre 0 e 100
- [x] Validacao fiscal minima para emissao
- [x] Aviso visual de servico fiscal incompleto
- [x] Lista com busca por nome e tags
- [x] Filtro por categoria
- [x] Filtro para mostrar arquivados
- [x] Paginacao server-side da lista
- [x] Cadastro inline de categoria com prevencao de nome duplicado

## Produtos
- [x] Validacao explicita de pertencimento do produto ao salao ao editar
- [x] Parsing padronizado de valores decimais
- [x] Validacao de estoque, custo e venda
- [x] Validacao de NCM, CEST e GTIN
- [x] Resumo de validacao no formulario
- [x] Lista com busca por nome, codigo e tags
- [x] Filtro por categoria
- [x] Filtro para mostrar arquivados
- [x] Paginacao server-side da lista
- [x] Acao visual padronizada de inativacao na lista
- [x] Cadastro inline de categoria com prevencao de nome duplicado

## Categorias de Produtos
- [x] Soft delete no lugar de exclusao fisica
- [x] Lista mostrando apenas categorias ativas por padrao
- [x] Filtro para exibir inativas
- [x] Busca por nome
- [x] Paginacao server-side da lista
- [x] Bloqueio de duplicidade por nome no mesmo salao
- [x] Cadastro e lista ajustados para trabalhar com status ativo/inativo

## Estruturais
- [x] Remocao de instanciacao manual de handlers nos cadastros e listas alterados
- [x] Uso de DI nos cadastros e listas alterados
- [x] DataAnnotations adicionadas aos models principais
- [x] Padronizacao de mensagens e tipos de alerta nas telas alteradas
- [x] Padronizacao de busca e paginacao nos modulos alterados
- [x] Cobertura de testes para filtros, exclusao logica e validacoes principais

## Validacao final
- [x] `dotnet build CorteCor.csproj -o tempbuild_cadastros_exec_final`
- [x] `dotnet build CorteCor.Tests\\CorteCor.Tests.csproj`
- [x] `dotnet test` direcionado para os testes de cadastro, handlers e DI
