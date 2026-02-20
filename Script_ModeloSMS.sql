-- Script de Inserção de Modelos SMS Padrão para o SMSMarket

-- Insere um modelo de boas-vindas para o salão 1 (altere ID do salão conforme necessidade)
INSERT INTO CorteCor_ModeloSMS (IdSalao, TipoEvento, Conteudo, Ativo, DataAtualizacao)
VALUES (1, 'BoasVindas', 'Ola {Nome}! Seja bem-vindo ao Salao {NomeSalao}. Estamos felizes em ter voce conosco!', 1, GETDATE());

-- Insere um modelo de lembrete de agendamento para o salão 1
INSERT INTO CorteCor_ModeloSMS (IdSalao, TipoEvento, Conteudo, Ativo, DataAtualizacao)
VALUES (1, 'LembreteAgendamento', 'Ola {Nome}, seu agendamento para {NomeServico} esta confirmado para {DataHora} no salao {NomeSalao}.', 1, GETDATE());

-- Insere um modelo de cancelamento de agendamento para o salão 1
INSERT INTO CorteCor_ModeloSMS (IdSalao, TipoEvento, Conteudo, Ativo, DataAtualizacao)
VALUES (1, 'CancelamentoAgendamento', 'Ola {Nome}, seu agendamento de {NomeServico} para {DataHora} foi cancelado. Aguardamos voce em breve.', 1, GETDATE());

-- Pode repetir os inserts para outros salões ou criar uma procedure que copia para os novos.
