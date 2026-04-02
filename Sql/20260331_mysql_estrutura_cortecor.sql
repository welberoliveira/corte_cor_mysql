SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE `CorteCor_Administrador` (
  `IdUsuario` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(200) NOT NULL,
  `Email` varchar(200) NOT NULL,
  `Senha` varchar(510) NOT NULL,
  `Perfil` varchar(100) NOT NULL,
  `Status` varchar(100) NOT NULL DEFAULT 'Ativo',
  `DataCriacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdUsuario`),
  UNIQUE KEY `UQ_CorteCor_Administrador_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Salao` (
  `IdSalao` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(510) NOT NULL,
  `Responsavel` varchar(510) NOT NULL,
  `Email` varchar(200) NOT NULL,
  `Telefone` varchar(40) DEFAULT NULL,
  `Endereco` varchar(510) DEFAULT NULL,
  `CNPJ` varchar(40) DEFAULT NULL,
  `Status` varchar(100) NOT NULL,
  `DataCadastro` datetime NOT NULL,
  `Observacao` longtext,
  `LimiteEnvioEmail` int NOT NULL DEFAULT 0,
  `LimiteEnvioSMS` int NOT NULL DEFAULT 0,
  `LimiteEnvioWhatsapp` int NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdSalao`),
  UNIQUE KEY `UQ_CorteCor_Salao_Email` (`Email`),
  UNIQUE KEY `UQ_CorteCor_Salao_CNPJ` (`CNPJ`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Usuario` (
  `IdUsuario` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(200) NOT NULL,
  `Email` varchar(200) DEFAULT NULL,
  `Telefone` varchar(30) DEFAULT NULL,
  `DataEntrada` date NOT NULL,
  `DataSaida` date DEFAULT NULL,
  `Status` varchar(100) NOT NULL DEFAULT 'Ativo',
  `CPF` varchar(28) NOT NULL DEFAULT '000.000.000-00',
  `Sobrenome` varchar(200) NOT NULL DEFAULT '',
  `Senha` varchar(510) NOT NULL DEFAULT 'Senha123',
  `IdSalao` int NOT NULL,
  PRIMARY KEY (`IdUsuario`),
  UNIQUE KEY `UQ_CorteCor_Usuario_Email_Salao` (`Email`,`IdSalao`),
  CONSTRAINT `FK_CorteCor_Usuario_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CategoriaProduto` (
  `IdCategoria` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `Nome` varchar(150) NOT NULL,
  `Ativo` tinyint(1) NOT NULL DEFAULT 1,
  `DataCadastro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdCategoria`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Servico` (
  `IdServico` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(300) NOT NULL,
  `Preco` decimal(10,2) NOT NULL,
  `Cor` varchar(40) DEFAULT NULL,
  `IdSalao` int NOT NULL,
  `Duracao` time NOT NULL DEFAULT '00:00:00',
  `CodigoTributacaoMunicipio` varchar(20) DEFAULT NULL,
  `Cnae` varchar(20) DEFAULT NULL,
  `AliquotaISS` decimal(5,2) DEFAULT NULL,
  `Tags` longtext,
  `Anotacoes` longtext,
  `ItemListaServicoLC116` varchar(10) DEFAULT NULL,
  `IdCnae` varchar(50) DEFAULT NULL,
  `CodTributacaoNacional` varchar(50) DEFAULT NULL,
  `CodNBS` varchar(50) DEFAULT NULL,
  `Arquivado` tinyint(1) NOT NULL DEFAULT 0,
  `PrecoCusto` decimal(18,2) DEFAULT NULL,
  `MargemContribuicao` decimal(18,2) DEFAULT NULL,
  `IdCategoria` int DEFAULT NULL,
  PRIMARY KEY (`IdServico`),
  CONSTRAINT `FK_CorteCor_Servico_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`),
  CONSTRAINT `FK_Servico_Categoria`
    FOREIGN KEY (`IdCategoria`) REFERENCES `CorteCor_CategoriaProduto` (`IdCategoria`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Funcionario` (
  `IdFuncionario` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(300) NOT NULL,
  `seg` tinyint(1) NOT NULL DEFAULT 0,
  `seg_ini` time DEFAULT NULL,
  `seg_fim` time DEFAULT NULL,
  `ter` tinyint(1) NOT NULL DEFAULT 0,
  `ter_ini` time DEFAULT NULL,
  `ter_fim` time DEFAULT NULL,
  `qua` tinyint(1) NOT NULL DEFAULT 0,
  `qua_ini` time DEFAULT NULL,
  `qua_fim` time DEFAULT NULL,
  `qui` tinyint(1) NOT NULL DEFAULT 0,
  `qui_ini` time DEFAULT NULL,
  `qui_fim` time DEFAULT NULL,
  `sex` tinyint(1) NOT NULL DEFAULT 0,
  `sex_ini` time DEFAULT NULL,
  `sex_fim` time DEFAULT NULL,
  `sab` tinyint(1) NOT NULL DEFAULT 0,
  `sab_ini` time DEFAULT NULL,
  `sab_fim` time DEFAULT NULL,
  `dom` tinyint(1) NOT NULL DEFAULT 0,
  `dom_ini` time DEFAULT NULL,
  `dom_fim` time DEFAULT NULL,
  `IdSalao` int DEFAULT NULL,
  PRIMARY KEY (`IdFuncionario`),
  CONSTRAINT `FK_CorteCor_Funcionario_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Pessoa` (
  `IdPessoa` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(300) NOT NULL,
  `Telefone` varchar(40) DEFAULT NULL,
  `Email` varchar(300) DEFAULT NULL,
  `DataNascimento` date DEFAULT NULL,
  `IdSalao` int NOT NULL,
  `Excluido` tinyint(1) DEFAULT 0,
  `CpfCnpj` varchar(20) DEFAULT NULL,
  `InscricaoEstadual` varchar(20) DEFAULT NULL,
  `InscricaoMunicipal` varchar(20) DEFAULT NULL,
  `Cep` varchar(10) DEFAULT NULL,
  `Logradouro` varchar(150) DEFAULT NULL,
  `Numero` varchar(10) DEFAULT NULL,
  `Complemento` varchar(50) DEFAULT NULL,
  `Bairro` varchar(100) DEFAULT NULL,
  `Cidade` varchar(100) DEFAULT NULL,
  `UF` varchar(2) DEFAULT NULL,
  `RazaoSocial` varchar(400) DEFAULT NULL,
  `NomeFantasia` varchar(400) DEFAULT NULL,
  `Cnae` varchar(20) DEFAULT NULL,
  `IsCliente` tinyint(1) DEFAULT 1,
  `IsFornecedor` tinyint(1) DEFAULT 0,
  `IsTransportador` tinyint(1) DEFAULT 0,
  `NomeContato` varchar(150) DEFAULT NULL,
  `Pais` varchar(100) DEFAULT NULL,
  `IdEstrangeiro` varchar(100) DEFAULT NULL,
  `EntCep` varchar(20) DEFAULT NULL,
  `EntUf` varchar(2) DEFAULT NULL,
  `EntCidade` varchar(100) DEFAULT NULL,
  `EntNome` varchar(150) DEFAULT NULL,
  `EntCpfCnpj` varchar(20) DEFAULT NULL,
  `EntInscricaoEstadual` varchar(50) DEFAULT NULL,
  `EntLogradouro` varchar(150) DEFAULT NULL,
  `EntNumero` varchar(20) DEFAULT NULL,
  `EntComplemento` varchar(100) DEFAULT NULL,
  `EntBairro` varchar(100) DEFAULT NULL,
  `EntEmail` varchar(150) DEFAULT NULL,
  `EntTelefone` varchar(20) DEFAULT NULL,
  `ConsumidorFinal` tinyint(1) DEFAULT 0,
  `IndicadorIE` int DEFAULT NULL,
  `IESubstTrib` varchar(50) DEFAULT NULL,
  `Suframa` varchar(50) DEFAULT NULL,
  `Tags` varchar(255) DEFAULT NULL,
  `DataComemorativa` date DEFAULT NULL,
  `DescricaoComemoracao` varchar(200) DEFAULT NULL,
  `BasesLegais` longtext,
  `Observacoes` longtext,
  PRIMARY KEY (`IdPessoa`),
  CONSTRAINT `FK_CorteCor_Pessoa_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Agendamento` (
  `IdAgendamento` int NOT NULL AUTO_INCREMENT,
  `DataHora` datetime(6) NOT NULL,
  `Status` varchar(100) NOT NULL DEFAULT 'Agendado',
  `IdServico` int NOT NULL,
  `IdPessoa` int NOT NULL,
  `IdFuncionario` int NOT NULL,
  `Excluido` tinyint(1) DEFAULT 0,
  PRIMARY KEY (`IdAgendamento`),
  KEY `IX_CorteCor_Agendamento_Funcionario_DataHora` (`IdFuncionario`,`DataHora`),
  CONSTRAINT `FK_CorteCor_Agendamento_Funcionario`
    FOREIGN KEY (`IdFuncionario`) REFERENCES `CorteCor_Funcionario` (`IdFuncionario`),
  CONSTRAINT `FK_CorteCor_Agendamento_Pessoa`
    FOREIGN KEY (`IdPessoa`) REFERENCES `CorteCor_Pessoa` (`IdPessoa`),
  CONSTRAINT `FK_CorteCor_Agendamento_Servico`
    FOREIGN KEY (`IdServico`) REFERENCES `CorteCor_Servico` (`IdServico`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_ConfigApi` (
  `IdApi` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `NomeApp` varchar(100) DEFAULT NULL,
  `ApiKey` char(36) DEFAULT (uuid()),
  `DataCriacao` datetime DEFAULT CURRENT_TIMESTAMP,
  `UltimoAcesso` datetime DEFAULT NULL,
  `Ativo` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`IdApi`),
  CONSTRAINT `FK_ConfigApi_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_ConfigGeral` (
  `IdSalao` int NOT NULL,
  `NomeFantasia` varchar(400) DEFAULT NULL,
  `LogoUrl` varchar(1000) DEFAULT NULL,
  `TemaCor` varchar(14) DEFAULT '#0d6efd',
  `ModoPDV` tinyint(1) DEFAULT 0,
  `ModoEstoque` tinyint(1) DEFAULT 0,
  `AgendamentoOnline` tinyint(1) DEFAULT 0,
  `MinutosAntecedencia` int DEFAULT 0,
  `DataAtualizacao` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdSalao`),
  CONSTRAINT `FK_ConfigGeral_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_ConfigPix` (
  `IdSalao` int NOT NULL,
  `ChavePix` varchar(200) DEFAULT NULL,
  `PSP` varchar(100) DEFAULT NULL,
  `ClientId` varchar(510) DEFAULT NULL,
  `ClientSecret` varchar(510) DEFAULT NULL,
  `Certificado` longblob,
  `Ativo` tinyint(1) DEFAULT 0,
  PRIMARY KEY (`IdSalao`),
  CONSTRAINT `FK_ConfigPix_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_ContaCaixa` (
  `IdConta` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `Nome` varchar(100) NOT NULL,
  `Tipo` varchar(40) DEFAULT NULL,
  `Banco` varchar(100) DEFAULT NULL,
  `Agencia` varchar(20) DEFAULT NULL,
  `Conta` varchar(40) DEFAULT NULL,
  `SaldoInicial` decimal(18,2) DEFAULT 0,
  `Ativo` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`IdConta`),
  KEY `IX_CorteCor_ContaCaixa_Salao_Ativo` (`IdSalao`,`Ativo`),
  CONSTRAINT `FK_ContaCaixa_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CrmEtapaFunil` (
  `IdEtapa` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `Nome` varchar(160) NOT NULL,
  `Ordem` int NOT NULL,
  `Ganha` tinyint(1) NOT NULL DEFAULT 0,
  `Perdida` tinyint(1) NOT NULL DEFAULT 0,
  `Ativa` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`IdEtapa`),
  KEY `IX_CorteCor_CrmEtapaFunil_Salao_Ordem` (`IdSalao`,`Ordem`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CrmPessoaPerfil` (
  `IdPerfil` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdPessoa` int NOT NULL,
  `StatusRelacionamento` varchar(80) NOT NULL DEFAULT 'Cliente',
  `OrigemLead` varchar(160) DEFAULT NULL,
  `Temperatura` varchar(40) NOT NULL DEFAULT 'Morno',
  `ScoreRelacionamento` int NOT NULL DEFAULT 0,
  `PermiteEmail` tinyint(1) NOT NULL DEFAULT 1,
  `PermiteSms` tinyint(1) NOT NULL DEFAULT 1,
  `PermiteWhatsapp` tinyint(1) NOT NULL DEFAULT 1,
  `NaoPerturbe` tinyint(1) NOT NULL DEFAULT 0,
  `UltimoContatoEm` datetime DEFAULT NULL,
  `ProximaAcaoEm` datetime DEFAULT NULL,
  `ObservacoesInternas` longtext,
  `DataAtualizacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdPerfil`),
  UNIQUE KEY `UX_CorteCor_CrmPessoaPerfil_Salao_Pessoa` (`IdSalao`,`IdPessoa`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CrmInteracao` (
  `IdInteracao` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdPessoa` int NOT NULL,
  `IdUsuario` int DEFAULT NULL,
  `Canal` varchar(60) NOT NULL,
  `Tipo` varchar(80) NOT NULL,
  `Assunto` varchar(320) NOT NULL,
  `Descricao` longtext,
  `DataInteracao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Referencia` varchar(200) DEFAULT NULL,
  `OrigemSistema` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdInteracao`),
  KEY `IX_CorteCor_CrmInteracao_Salao_Pessoa_Data` (`IdSalao`,`IdPessoa`,`DataInteracao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CrmOportunidade` (
  `IdOportunidade` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdPessoa` int NOT NULL,
  `IdEtapa` int NOT NULL,
  `Titulo` varchar(320) NOT NULL,
  `Descricao` longtext,
  `ValorEstimado` decimal(18,2) NOT NULL DEFAULT 0,
  `Probabilidade` int NOT NULL DEFAULT 0,
  `Status` varchar(40) NOT NULL DEFAULT 'Aberta',
  `Origem` varchar(160) DEFAULT NULL,
  `PrevisaoFechamento` date DEFAULT NULL,
  `DataCriacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `DataAtualizacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `DataFechamento` datetime DEFAULT NULL,
  PRIMARY KEY (`IdOportunidade`),
  KEY `IX_CorteCor_CrmOportunidade_Salao_Pessoa` (`IdSalao`,`IdPessoa`),
  KEY `IX_CorteCor_CrmOportunidade_Salao_Status` (`IdSalao`,`Status`,`IdEtapa`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CrmTarefa` (
  `IdTarefa` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdPessoa` int DEFAULT NULL,
  `IdUsuarioResponsavel` int DEFAULT NULL,
  `Titulo` varchar(320) NOT NULL,
  `Descricao` longtext,
  `Prioridade` varchar(40) NOT NULL DEFAULT 'Media',
  `Status` varchar(40) NOT NULL DEFAULT 'Aberta',
  `CanalSugerido` varchar(40) DEFAULT NULL,
  `DataVencimento` datetime NOT NULL,
  `DataConclusao` datetime DEFAULT NULL,
  `DataCriacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdTarefa`),
  KEY `IX_CorteCor_CrmTarefa_Salao_Pessoa` (`IdSalao`,`IdPessoa`),
  KEY `IX_CorteCor_CrmTarefa_Salao_Status_Vencimento` (`IdSalao`,`Status`,`DataVencimento`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CrmCampanha` (
  `IdCampanha` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `Nome` varchar(240) NOT NULL,
  `Canal` varchar(40) NOT NULL,
  `Segmento` varchar(80) NOT NULL,
  `FiltroTag` varchar(240) DEFAULT NULL,
  `DiasInatividade` int DEFAULT NULL,
  `IdPessoa` int DEFAULT NULL,
  `Assunto` varchar(320) DEFAULT NULL,
  `Conteudo` longtext NOT NULL,
  `Status` varchar(40) NOT NULL DEFAULT 'Rascunho',
  `TotalDestinatarios` int NOT NULL DEFAULT 0,
  `TotalSucesso` int NOT NULL DEFAULT 0,
  `TotalFalha` int NOT NULL DEFAULT 0,
  `UltimoEnvioEm` datetime DEFAULT NULL,
  `DataCriacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdCampanha`),
  KEY `IX_CorteCor_CrmCampanha_Salao_DataCriacao` (`IdSalao`,`DataCriacao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_CrmCampanhaDestino` (
  `IdDestino` int NOT NULL AUTO_INCREMENT,
  `IdCampanha` int NOT NULL,
  `IdSalao` int NOT NULL,
  `IdPessoa` int NOT NULL,
  `Canal` varchar(40) NOT NULL,
  `Destino` varchar(360) NOT NULL,
  `Status` varchar(40) NOT NULL,
  `MensagemErro` longtext,
  `DataEnvio` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdDestino`),
  KEY `IX_CorteCor_CrmCampanhaDestino_Salao_Campanha_DataEnvio` (`IdSalao`,`IdCampanha`,`DataEnvio`),
  KEY `IX_CorteCor_CrmCampanhaDestino_Salao_Pessoa` (`IdSalao`,`IdPessoa`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_FinanceiroTitulo` (
  `IdTitulo` char(36) NOT NULL,
  `IdSalao` int NOT NULL,
  `Tipo` varchar(20) NOT NULL,
  `Origem` varchar(80) NOT NULL,
  `IdPessoa` int DEFAULT NULL,
  `IdAgendamento` int DEFAULT NULL,
  `IdPagamento` char(36) DEFAULT NULL,
  `IdPlano` int DEFAULT NULL,
  `IdConta` int DEFAULT NULL,
  `Descricao` varchar(320) NOT NULL,
  `Documento` varchar(120) DEFAULT NULL,
  `Status` varchar(40) NOT NULL,
  `ValorOriginal` decimal(18,2) NOT NULL,
  `ValorLiquidado` decimal(18,2) NOT NULL DEFAULT 0,
  `ValorAberto` decimal(18,2) NOT NULL DEFAULT 0,
  `DataCompetencia` date NOT NULL,
  `DataVencimento` date NOT NULL,
  `DataLiquidacao` datetime DEFAULT NULL,
  `Conciliado` tinyint(1) NOT NULL DEFAULT 0,
  `Observacoes` longtext,
  `DataCriacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `DataAtualizacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IdVendaProduto` int DEFAULT NULL,
  PRIMARY KEY (`IdTitulo`),
  UNIQUE KEY `UX_CorteCor_FinanceiroTitulo_Salao_Pagamento` (`IdSalao`,`IdPagamento`),
  KEY `IX_CorteCor_FinanceiroTitulo_IdVendaProduto` (`IdSalao`,`IdVendaProduto`),
  KEY `IX_CorteCor_FinanceiroTitulo_Salao_Status_Vencimento` (`IdSalao`,`Status`,`DataVencimento`),
  KEY `IX_CorteCor_FinanceiroTitulo_Salao_Tipo_Liquidacao` (`IdSalao`,`Tipo`,`DataLiquidacao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_FornecedoresEmail` (
  `IdFornecedor` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(100) NOT NULL,
  `ApiKey` varchar(255) DEFAULT NULL,
  `ApiSecret` varchar(255) DEFAULT NULL,
  `Endpoint` varchar(255) DEFAULT NULL,
  `RemetenteNome` varchar(100) DEFAULT NULL,
  `RemetenteEmail` varchar(100) DEFAULT NULL,
  `Ativo` tinyint(1) DEFAULT 0,
  `DataCriacao` datetime DEFAULT CURRENT_TIMESTAMP,
  `DataAtualizacao` datetime DEFAULT NULL,
  PRIMARY KEY (`IdFornecedor`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_FornecedoresSMS` (
  `IdFornecedor` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(100) NOT NULL,
  `ApiKey` varchar(255) DEFAULT NULL,
  `ApiSecret` varchar(255) DEFAULT NULL,
  `Endpoint` varchar(255) DEFAULT NULL,
  `Remetente` varchar(50) DEFAULT NULL,
  `Ativo` tinyint(1) DEFAULT 0,
  `DataCriacao` datetime DEFAULT CURRENT_TIMESTAMP,
  `DataAtualizacao` datetime DEFAULT NULL,
  PRIMARY KEY (`IdFornecedor`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_FornecedoresWhatsapp` (
  `IdFornecedor` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(100) NOT NULL,
  `ApiKey` varchar(255) DEFAULT NULL,
  `ApiSecret` varchar(255) DEFAULT NULL,
  `Endpoint` varchar(255) DEFAULT NULL,
  `InstanceId` varchar(100) DEFAULT NULL,
  `Token` varchar(255) DEFAULT NULL,
  `Ativo` tinyint(1) DEFAULT 0,
  `DataCriacao` datetime DEFAULT CURRENT_TIMESTAMP,
  `DataAtualizacao` datetime DEFAULT NULL,
  PRIMARY KEY (`IdFornecedor`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Funcionario_Servico` (
  `IdFuncionario` int NOT NULL,
  `IdServico` int NOT NULL,
  PRIMARY KEY (`IdFuncionario`,`IdServico`),
  CONSTRAINT `FK_FuncionarioServico_Funcionario`
    FOREIGN KEY (`IdFuncionario`) REFERENCES `CorteCor_Funcionario` (`IdFuncionario`),
  CONSTRAINT `FK_FuncionarioServico_Servico`
    FOREIGN KEY (`IdServico`) REFERENCES `CorteCor_Servico` (`IdServico`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_ItemListaServico` (
  `IdItemListaServico` int NOT NULL AUTO_INCREMENT,
  `Codigo` varchar(10) NOT NULL,
  `Descricao` varchar(500) NOT NULL,
  PRIMARY KEY (`IdItemListaServico`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_LembreteConfig` (
  `IdConfig` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `AntecedenciaValor` int NOT NULL,
  `AntecedenciaUnidade` varchar(10) NOT NULL,
  `IdModeloEmail` int DEFAULT NULL,
  `Ativo` tinyint(1) NOT NULL DEFAULT 1,
  `DataCriacao` datetime DEFAULT CURRENT_TIMESTAMP,
  `DataInicio` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `DataFim` datetime DEFAULT NULL,
  `TipoLembrete` varchar(40) NOT NULL DEFAULT 'Email',
  `IdModeloSMS` int DEFAULT NULL,
  PRIMARY KEY (`IdConfig`),
  KEY `IX_LembreteConfig_Tipo` (`TipoLembrete`),
  CONSTRAINT `FK_LembreteConfig_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_LembreteAgendado` (
  `IdLembrete` int NOT NULL AUTO_INCREMENT,
  `IdAgendamento` int NOT NULL,
  `IdConfig` int NOT NULL,
  `DataEnvioProgramada` datetime NOT NULL,
  `Status` varchar(20) NOT NULL DEFAULT 'Pendente',
  `Tentativas` int DEFAULT 0,
  `UltimoErro` longtext,
  `DataEnvioReal` datetime DEFAULT NULL,
  `DataCriacao` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdLembrete`),
  KEY `IX_LembreteAgendado_Status_DataEnvio` (`Status`,`DataEnvioProgramada`),
  CONSTRAINT `FK_LembreteAgendado_Agendamento`
    FOREIGN KEY (`IdAgendamento`) REFERENCES `CorteCor_Agendamento` (`IdAgendamento`),
  CONSTRAINT `FK_LembreteAgendado_Config`
    FOREIGN KEY (`IdConfig`) REFERENCES `CorteCor_LembreteConfig` (`IdConfig`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_LogAcessos` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Usuario` varchar(400) NOT NULL,
  `DataHora` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IP_Origem` varchar(45) NOT NULL,
  `CredencialUsada` varchar(400) DEFAULT NULL,
  `Sucesso` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_LogEnvioEmail` (
  `IdLog` int NOT NULL AUTO_INCREMENT,
  `IdLembrete` int NOT NULL,
  `IdAgendamento` int NOT NULL,
  `DataEnvio` datetime NOT NULL,
  `Destinatario` varchar(200) NOT NULL,
  `Assunto` varchar(200) NOT NULL,
  `Status` varchar(50) NOT NULL,
  `MensagemErro` longtext,
  `TipoLembrete` varchar(40) NOT NULL DEFAULT 'Email',
  `Telefone` varchar(40) DEFAULT NULL,
  PRIMARY KEY (`IdLog`),
  KEY `IX_LogEnvio_Tipo` (`TipoLembrete`),
  KEY `IX_LogEnvioEmail_DataEnvio` (`DataEnvio`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_MeioPagamento` (
  `IdMeioPagamento` int NOT NULL AUTO_INCREMENT,
  `Nome` varchar(160) NOT NULL,
  `Tipo` varchar(60) NOT NULL,
  `Gateway` varchar(100) NOT NULL,
  `PermiteParcelamento` tinyint(1) NOT NULL DEFAULT 0,
  `ParcelasMax` tinyint DEFAULT NULL,
  `TaxaPercentual` decimal(6,3) NOT NULL DEFAULT 0,
  `TaxaFixa` decimal(10,2) NOT NULL DEFAULT 0,
  `PrazoRecebimentoDias` smallint NOT NULL DEFAULT 0,
  `Ativo` tinyint(1) NOT NULL DEFAULT 1,
  `IdSalao` int NOT NULL,
  `DataCadastro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `MpAccessTokenProd` varchar(200) DEFAULT NULL,
  `MpAccessTokenSandbox` varchar(200) DEFAULT NULL,
  `MpPublicKeyProd` varchar(200) DEFAULT NULL,
  `MpPublicKeySandbox` varchar(200) DEFAULT NULL,
  `MpProduction` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdMeioPagamento`),
  UNIQUE KEY `UQ_CorteCor_MeioPagamento_Salao_Nome` (`IdSalao`,`Nome`),
  CONSTRAINT `FK_CorteCor_MeioPagamento_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_ModeloEmail` (
  `IdModelo` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `TipoEvento` varchar(100) NOT NULL,
  `Assunto` varchar(510) NOT NULL,
  `CorpoHTML` longtext NOT NULL,
  `Ativo` tinyint(1) NOT NULL DEFAULT 1,
  `DataCriacao` datetime DEFAULT CURRENT_TIMESTAMP,
  `DataAtualizacao` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdModelo`),
  UNIQUE KEY `UQ_ModeloEmail_Salao_Evento` (`IdSalao`,`TipoEvento`),
  KEY `IX_ModeloEmail_Salao_Evento` (`IdSalao`,`TipoEvento`),
  CONSTRAINT `FK_CorteCor_ModeloEmail_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_ModeloSMS` (
  `IdModelo` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `TipoEvento` varchar(100) NOT NULL,
  `Conteudo` varchar(320) NOT NULL,
  `Ativo` tinyint(1) NOT NULL DEFAULT 1,
  `DataAtualizacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdModelo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Produto` (
  `IdProduto` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `Nome` varchar(200) NOT NULL,
  `CodigoProprio` varchar(50) DEFAULT NULL,
  `IdCategoria` int DEFAULT NULL,
  `Tags` longtext,
  `TipoUso` varchar(50) DEFAULT NULL,
  `Arquivado` tinyint(1) NOT NULL DEFAULT 0,
  `Anotacoes` longtext,
  `PrecoCusto` decimal(18,2) DEFAULT NULL,
  `PrecoVenda` decimal(18,2) NOT NULL,
  `MargemContribuicao` decimal(5,2) DEFAULT NULL,
  `ControlarEstoque` tinyint(1) NOT NULL DEFAULT 0,
  `EstoqueAtual` decimal(18,3) DEFAULT 0,
  `EstoqueMinimo` decimal(18,3) DEFAULT 0,
  `Origem` int DEFAULT NULL,
  `ReferenciaEAN` varchar(50) DEFAULT NULL,
  `PesoLiquido` decimal(18,3) DEFAULT NULL,
  `PesoBruto` decimal(18,3) DEFAULT NULL,
  `NCM` varchar(20) DEFAULT NULL,
  `CEST` varchar(20) DEFAULT NULL,
  `UnidadeComercial` varchar(10) DEFAULT NULL,
  `ExcecaoIPI` int DEFAULT NULL,
  `CodBeneficioFiscalUF` varchar(20) DEFAULT NULL,
  `UnidadeTributadaDiferente` tinyint(1) NOT NULL DEFAULT 0,
  `EANTributada` varchar(50) DEFAULT NULL,
  `UnidadeTributada` varchar(10) DEFAULT NULL,
  `QuantidadeTributada` decimal(18,3) DEFAULT NULL,
  `IgnorarTribPrecoVenda` tinyint(1) NOT NULL DEFAULT 0,
  `AnotacoesFiscaisNFe` longtext,
  `GrupoTributarioVinculado` int DEFAULT NULL,
  `DataCadastro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Excluido` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdProduto`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_MovimentoEstoque` (
  `IdMovimento` char(36) NOT NULL,
  `IdSalao` int NOT NULL,
  `IdProduto` int NOT NULL,
  `IdVendaProduto` int DEFAULT NULL,
  `TipoMovimento` varchar(40) NOT NULL,
  `Origem` varchar(80) NOT NULL,
  `Quantidade` decimal(18,3) NOT NULL,
  `SaldoAnterior` decimal(18,3) NOT NULL,
  `SaldoPosterior` decimal(18,3) NOT NULL,
  `Observacao` varchar(1000) DEFAULT NULL,
  `UsuarioOperador` varchar(320) DEFAULT NULL,
  `DataMovimento` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`IdMovimento`),
  KEY `IX_CorteCor_MovimentoEstoque_IdSalao_DataMovimento` (`IdSalao`,`DataMovimento`),
  CONSTRAINT `FK_CorteCor_MovimentoEstoque_Produto`
    FOREIGN KEY (`IdProduto`) REFERENCES `CorteCor_Produto` (`IdProduto`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_NotaFiscal` (
  `IdNotaFiscal` char(36) NOT NULL DEFAULT (uuid()),
  `IdSalao` int NOT NULL,
  `IdAgendamento` int DEFAULT NULL,
  `IdVendaProduto` int DEFAULT NULL,
  `TipoNota` varchar(10) NOT NULL,
  `Ambiente` int NOT NULL,
  `Numero` int NOT NULL,
  `Serie` int NOT NULL,
  `ValorTotal` decimal(18,2) NOT NULL,
  `Status` varchar(20) NOT NULL,
  `ChaveAcesso` varchar(80) DEFAULT NULL,
  `NumeroRecibo` varchar(50) DEFAULT NULL,
  `ProtocoloAutorizacao` varchar(255) DEFAULT NULL,
  `JustificativaRejeicao` varchar(500) DEFAULT NULL,
  `XmlEnvio` longtext,
  `XmlRetorno` longtext,
  `DataEmissao` datetime NOT NULL,
  `DataAtualizacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ChaveAcessoNacional` varchar(120) DEFAULT NULL,
  `NumeroNFSeNacional` varchar(60) DEFAULT NULL,
  PRIMARY KEY (`IdNotaFiscal`),
  UNIQUE KEY `UX_CorteCor_NotaFiscal_Salao_Tipo_Ambiente_Serie_Numero` (`IdSalao`,`TipoNota`,`Ambiente`,`Serie`,`Numero`),
  KEY `IX_CorteCor_NotaFiscal_ChaveAcesso` (`ChaveAcesso`),
  KEY `IX_CorteCor_NotaFiscal_ChaveAcessoNacional` (`ChaveAcessoNacional`),
  KEY `IX_CorteCor_NotaFiscal_Salao_Data` (`IdSalao`,`DataEmissao`),
  KEY `IX_CorteCor_NotaFiscal_Serie_Numero` (`IdSalao`,`TipoNota`,`Ambiente`,`Serie`,`Numero`),
  KEY `IX_CorteCor_NotaFiscal_Status_Tipo` (`Status`,`TipoNota`,`IdSalao`),
  CONSTRAINT `FK_NotaFiscal_Agendamento`
    FOREIGN KEY (`IdAgendamento`) REFERENCES `CorteCor_Agendamento` (`IdAgendamento`),
  CONSTRAINT `FK_NotaFiscal_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_NotaFiscalEvento` (
  `IdEvento` char(36) NOT NULL DEFAULT (uuid()),
  `IdNotaFiscal` char(36) NOT NULL,
  `IdSalao` int NOT NULL,
  `TipoEvento` varchar(50) NOT NULL,
  `Justificativa` varchar(255) NOT NULL,
  `ProtocoloEvento` varchar(255) DEFAULT NULL,
  `XmlEnvio` longtext,
  `XmlRetorno` longtext,
  `Status` varchar(100) NOT NULL,
  `DataRegistro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdEvento`),
  KEY `IX_CorteCor_NotaFiscalEvento_Nota` (`IdNotaFiscal`,`DataRegistro`),
  CONSTRAINT `FK_Evento_NotaFiscal`
    FOREIGN KEY (`IdNotaFiscal`) REFERENCES `CorteCor_NotaFiscal` (`IdNotaFiscal`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_NotaFiscalInutilizacao` (
  `IdInutilizacao` char(36) NOT NULL DEFAULT (uuid()),
  `IdSalao` int NOT NULL,
  `Ano` int NOT NULL,
  `Modelo` int NOT NULL,
  `Serie` int NOT NULL,
  `NumeroInicial` int NOT NULL,
  `NumeroFinal` int NOT NULL,
  `Justificativa` varchar(255) NOT NULL,
  `Status` varchar(20) NOT NULL,
  `Protocolo` varchar(50) DEFAULT NULL,
  `XmlRetorno` longtext,
  `DataInutilizacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdInutilizacao`),
  KEY `IX_CorteCor_NotaFiscalInutilizacao_Salao_Data` (`IdSalao`,`DataInutilizacao`),
  CONSTRAINT `FK_Inutilizacao_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_NotaFiscalLog` (
  `IdLog` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdNotaFiscal` char(36) DEFAULT NULL,
  `IdAgendamento` int DEFAULT NULL,
  `DataHora` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `TipoEvento` varchar(255) NOT NULL,
  `RequestPayload` longtext,
  `ResponsePayload` longtext,
  `CodigoErro` varchar(255) DEFAULT NULL,
  `Mensagem` longtext,
  `Usuario` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`IdLog`),
  KEY `IX_CorteCor_NotaFiscalLog_Nota` (`IdNotaFiscal`,`DataHora`),
  KEY `IX_CorteCor_NotaFiscalLog_Salao_Data` (`IdSalao`,`DataHora`),
  CONSTRAINT `FK_CorteCor_NotaFiscalLog_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Pagamento` (
  `IdPagamento` char(36) NOT NULL DEFAULT (uuid()),
  `IdAgendamento` int NOT NULL,
  `Ativo` tinyint(1) NOT NULL DEFAULT 1,
  `Status` varchar(20) NOT NULL DEFAULT 'Pendente',
  `Valor` decimal(18,2) NOT NULL,
  `Moeda` char(3) NOT NULL DEFAULT 'BRL',
  `Descricao` varchar(400) DEFAULT NULL,
  `MercadoPagoPreferenceId` varchar(160) DEFAULT NULL,
  `MercadoPagoPaymentId` varchar(60) DEFAULT NULL,
  `CheckoutUrl` varchar(1000) DEFAULT NULL,
  `MpStatus` varchar(60) DEFAULT NULL,
  `MpStatusDetail` varchar(160) DEFAULT NULL,
  `CriadoEm` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `AtualizadoEm` datetime(6) DEFAULT NULL,
  `PagoEm` datetime(6) DEFAULT NULL,
  `JsonResposta` longtext,
  `DataExpiracao` datetime(6) DEFAULT NULL,
  `UrlPagamento` varchar(1000) DEFAULT NULL,
  `Tipo` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`IdPagamento`),
  UNIQUE KEY `UX_CorteCor_Pagamento_Agendamento_Ativo` (`IdAgendamento`),
  KEY `IX_CorteCor_Pagamento_MercadoPagoPaymentId` (`MercadoPagoPaymentId`),
  CONSTRAINT `FK_CorteCor_Pagamento_Agendamento`
    FOREIGN KEY (`IdAgendamento`) REFERENCES `CorteCor_Agendamento` (`IdAgendamento`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Pagamento_Log` (
  `IdLog` int NOT NULL AUTO_INCREMENT,
  `IdPagamento` char(36) NOT NULL,
  `DataLog` datetime DEFAULT CURRENT_TIMESTAMP,
  `TipoEvento` varchar(100) DEFAULT NULL,
  `Conteudo` longtext,
  PRIMARY KEY (`IdLog`),
  CONSTRAINT `FK_CorteCor_Pagamento_Log_IdPagamento`
    FOREIGN KEY (`IdPagamento`) REFERENCES `CorteCor_Pagamento` (`IdPagamento`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_Pedido` (
  `IdPedido` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdPessoa` int DEFAULT NULL,
  `IdMeioPagamento` int DEFAULT NULL,
  `Status` varchar(80) NOT NULL DEFAULT 'Aberto',
  `TipoPagamento` varchar(160) DEFAULT NULL,
  `ValidoAte` date NOT NULL,
  `SubtotalProdutos` decimal(18,2) NOT NULL DEFAULT 0,
  `SubtotalServicos` decimal(18,2) NOT NULL DEFAULT 0,
  `Desconto` decimal(18,2) NOT NULL DEFAULT 0,
  `Acrescimo` decimal(18,2) NOT NULL DEFAULT 0,
  `ValorTotal` decimal(18,2) NOT NULL,
  `Observacoes` varchar(2000) DEFAULT NULL,
  `Origem` varchar(80) NOT NULL DEFAULT 'Manual',
  `UsuarioOperador` varchar(320) DEFAULT NULL,
  `IdVendaProduto` int DEFAULT NULL,
  `DataPedido` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `DataCriacao` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `DataAtualizacao` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`IdPedido`),
  KEY `IX_CorteCor_Pedido_IdSalao_DataPedido` (`IdSalao`,`DataPedido`),
  KEY `IX_CorteCor_Pedido_IdSalao_Status_ValidoAte` (`IdSalao`,`Status`,`ValidoAte`),
  KEY `IX_CorteCor_Pedido_IdVendaProduto` (`IdSalao`,`IdVendaProduto`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_PedidoItem` (
  `IdItemPedido` int NOT NULL AUTO_INCREMENT,
  `IdPedido` int NOT NULL,
  `IdSalao` int NOT NULL,
  `TipoItem` varchar(40) NOT NULL,
  `IdProduto` int DEFAULT NULL,
  `IdServico` int DEFAULT NULL,
  `Descricao` varchar(400) NOT NULL,
  `Quantidade` decimal(18,3) NOT NULL,
  `ValorUnitario` decimal(18,2) NOT NULL,
  `ValorTotal` decimal(18,2) NOT NULL,
  `Unidade` varchar(20) NOT NULL DEFAULT 'UN',
  `ControlaEstoque` tinyint(1) NOT NULL DEFAULT 0,
  `CodigoTributacaoMunicipio` varchar(40) DEFAULT NULL,
  `AliquotaIss` decimal(8,2) DEFAULT NULL,
  `Ncm` varchar(40) DEFAULT NULL,
  `Cfop` varchar(20) DEFAULT NULL,
  PRIMARY KEY (`IdItemPedido`),
  KEY `IX_CorteCor_PedidoItem_IdPedido` (`IdPedido`),
  CONSTRAINT `FK_CorteCor_PedidoItem_Pedido`
    FOREIGN KEY (`IdPedido`) REFERENCES `CorteCor_Pedido` (`IdPedido`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_PlanoContas` (
  `IdPlano` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `Codigo` varchar(40) DEFAULT NULL,
  `Descricao` varchar(200) NOT NULL,
  `Tipo` char(1) NOT NULL,
  `Ativo` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`IdPlano`),
  KEY `IX_CorteCor_PlanoContas_Salao_Tipo` (`IdSalao`,`Tipo`,`Ativo`),
  CONSTRAINT `FK_PlanoContas_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_SalaoConfigFiscal` (
  `IdConfigFiscal` char(36) NOT NULL DEFAULT (uuid()),
  `IdSalao` int NOT NULL,
  `Cnpj` varchar(14) NOT NULL,
  `RazaoSocial` varchar(150) NOT NULL,
  `InscricaoEstadual` varchar(20) DEFAULT NULL,
  `InscricaoMunicipal` varchar(20) DEFAULT NULL,
  `Ambiente` int NOT NULL DEFAULT 2,
  `CodigoMunicipioIBGE` int NOT NULL,
  `CodigoUFIBGE` int NOT NULL,
  `RegimeTributario` int NOT NULL,
  `CertificadoPfx` longblob,
  `CertificadoSenha` varbinary(500),
  `CertificadoValidade` datetime DEFAULT NULL,
  `DataAtualizacao` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `EmissaoAutomatica` tinyint(1) NOT NULL DEFAULT 0,
  `TokenNfse` varchar(200) DEFAULT NULL,
  `CSC` varchar(100) DEFAULT NULL,
  `IdCSC` varchar(40) DEFAULT NULL,
  `SerieNFCe` int NOT NULL DEFAULT 1,
  `NumeroNFCe` int NOT NULL DEFAULT 1,
  `SerieNFSe` int NOT NULL DEFAULT 1,
  `NumeroNFSe` int NOT NULL DEFAULT 1,
  `RegimeEspecialTributacao` int NOT NULL DEFAULT 0,
  `IssExigibilidade` int NOT NULL DEFAULT 1,
  `IssRetido` int NOT NULL DEFAULT 2,
  `EnderecoLogradouro` varchar(300) DEFAULT NULL,
  `EnderecoNumero` varchar(20) DEFAULT NULL,
  `EnderecoBairro` varchar(200) DEFAULT NULL,
  `EnderecoCep` varchar(20) DEFAULT NULL,
  `Telefone` varchar(40) DEFAULT NULL,
  `Email` varchar(200) DEFAULT NULL,
  `EnderecoCidade` varchar(200) DEFAULT NULL,
  `EnderecoUF` char(2) DEFAULT NULL,
  PRIMARY KEY (`IdConfigFiscal`),
  UNIQUE KEY `UQ_CorteCor_SalaoConfigFiscal_IdSalao` (`IdSalao`),
  KEY `UX_CorteCor_SalaoConfigFiscal_IdSalao` (`IdSalao`),
  CONSTRAINT `FK_ConfigFiscal_Salao`
    FOREIGN KEY (`IdSalao`) REFERENCES `CorteCor_Salao` (`IdSalao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_VendaProduto` (
  `IdVendaProduto` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdPessoa` int DEFAULT NULL,
  `IdMeioPagamento` int DEFAULT NULL,
  `Status` varchar(80) NOT NULL,
  `TipoPagamento` varchar(160) DEFAULT NULL,
  `RecebidoNaHora` tinyint(1) NOT NULL DEFAULT 1,
  `SolicitarEmissaoFiscalServico` tinyint(1) NOT NULL DEFAULT 0,
  `SubtotalProdutos` decimal(18,2) NOT NULL DEFAULT 0,
  `SubtotalServicos` decimal(18,2) NOT NULL DEFAULT 0,
  `Desconto` decimal(18,2) NOT NULL DEFAULT 0,
  `Acrescimo` decimal(18,2) NOT NULL DEFAULT 0,
  `ValorTotal` decimal(18,2) NOT NULL,
  `Observacoes` varchar(2000) DEFAULT NULL,
  `Origem` varchar(80) NOT NULL DEFAULT 'Manual',
  `UsuarioOperador` varchar(320) DEFAULT NULL,
  `DataVenda` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `DataCriacao` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `DataAtualizacao` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`IdVendaProduto`),
  KEY `IX_CorteCor_VendaProduto_IdSalao_DataVenda` (`IdSalao`,`DataVenda`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_VendaProdutoItem` (
  `IdItemVenda` int NOT NULL AUTO_INCREMENT,
  `IdVendaProduto` int NOT NULL,
  `IdSalao` int NOT NULL,
  `TipoItem` varchar(40) NOT NULL,
  `IdProduto` int DEFAULT NULL,
  `IdServico` int DEFAULT NULL,
  `Descricao` varchar(400) NOT NULL,
  `Quantidade` decimal(18,3) NOT NULL,
  `ValorUnitario` decimal(18,2) NOT NULL,
  `ValorTotal` decimal(18,2) NOT NULL,
  `Unidade` varchar(20) NOT NULL DEFAULT 'UN',
  `ControlaEstoque` tinyint(1) NOT NULL DEFAULT 0,
  `CodigoTributacaoMunicipio` varchar(40) DEFAULT NULL,
  `AliquotaIss` decimal(8,2) DEFAULT NULL,
  `Ncm` varchar(40) DEFAULT NULL,
  `Cfop` varchar(20) DEFAULT NULL,
  `QuantidadeCancelada` decimal(18,3) NOT NULL DEFAULT 0,
  `QuantidadeDevolvida` decimal(18,3) NOT NULL DEFAULT 0,
  `QuantidadeTrocada` decimal(18,3) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdItemVenda`),
  KEY `IX_CorteCor_VendaProdutoItem_IdVendaProduto` (`IdVendaProduto`),
  CONSTRAINT `FK_CorteCor_VendaProdutoItem_Venda`
    FOREIGN KEY (`IdVendaProduto`) REFERENCES `CorteCor_VendaProduto` (`IdVendaProduto`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_VendaPosVenda` (
  `IdPosVenda` int NOT NULL AUTO_INCREMENT,
  `IdSalao` int NOT NULL,
  `IdVendaProduto` int NOT NULL,
  `TipoOperacao` varchar(60) NOT NULL,
  `Status` varchar(40) NOT NULL DEFAULT 'Processada',
  `ValorCredito` decimal(18,2) NOT NULL DEFAULT 0,
  `ValorReposicao` decimal(18,2) NOT NULL DEFAULT 0,
  `DiferencaFinanceira` decimal(18,2) NOT NULL DEFAULT 0,
  `Observacoes` varchar(2000) DEFAULT NULL,
  `UsuarioOperador` varchar(320) DEFAULT NULL,
  `DataOperacao` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`IdPosVenda`),
  KEY `IX_CorteCor_VendaPosVenda_IdSalao_IdVendaProduto` (`IdSalao`,`IdVendaProduto`,`DataOperacao`),
  CONSTRAINT `FK_CorteCor_VendaPosVenda_Venda`
    FOREIGN KEY (`IdVendaProduto`) REFERENCES `CorteCor_VendaProduto` (`IdVendaProduto`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CorteCor_VendaPosVendaItem` (
  `IdPosVendaItem` int NOT NULL AUTO_INCREMENT,
  `IdPosVenda` int NOT NULL,
  `IdSalao` int NOT NULL,
  `IdVendaProduto` int NOT NULL,
  `IdItemVenda` int DEFAULT NULL,
  `TipoRegistro` varchar(40) NOT NULL,
  `TipoItem` varchar(40) NOT NULL,
  `IdProduto` int DEFAULT NULL,
  `IdServico` int DEFAULT NULL,
  `Descricao` varchar(400) NOT NULL,
  `Quantidade` decimal(18,3) NOT NULL,
  `ValorUnitario` decimal(18,2) NOT NULL,
  `ValorTotal` decimal(18,2) NOT NULL,
  `Unidade` varchar(20) NOT NULL DEFAULT 'UN',
  `ControlaEstoque` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdPosVendaItem`),
  KEY `IX_CorteCor_VendaPosVendaItem_IdPosVenda` (`IdPosVenda`),
  CONSTRAINT `FK_CorteCor_VendaPosVendaItem_PosVenda`
    FOREIGN KEY (`IdPosVenda`) REFERENCES `CorteCor_VendaPosVenda` (`IdPosVenda`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;
