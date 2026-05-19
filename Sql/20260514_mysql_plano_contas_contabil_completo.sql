-- Plano de contas contabil completo para DRE.
-- Execute este script no MySQL depois do backup do banco.
-- Ele e idempotente: usa Codigo + IdSalao como chave logica e nao duplica contas.

DELIMITER $$

DROP PROCEDURE IF EXISTS CorteCor_AddPlanoContasColumn $$
CREATE PROCEDURE CorteCor_AddPlanoContasColumn(IN pColumnName VARCHAR(64), IN pDefinition VARCHAR(500))
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'CorteCor_PlanoContas'
          AND COLUMN_NAME = pColumnName
    ) THEN
        SET @sql = CONCAT('ALTER TABLE CorteCor_PlanoContas ADD COLUMN ', pDefinition);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END $$

DROP PROCEDURE IF EXISTS CorteCor_AddPlanoContasIndex $$
CREATE PROCEDURE CorteCor_AddPlanoContasIndex(IN pIndexName VARCHAR(64), IN pDefinition VARCHAR(500))
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'CorteCor_PlanoContas'
          AND INDEX_NAME = pIndexName
    ) THEN
        SET @sql = CONCAT('ALTER TABLE CorteCor_PlanoContas ADD INDEX ', pDefinition);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END $$

DELIMITER ;

CALL CorteCor_AddPlanoContasColumn('Nome', 'Nome VARCHAR(200) NULL AFTER Descricao');
CALL CorteCor_AddPlanoContasColumn('IdPlanoPai', 'IdPlanoPai INT NULL AFTER Nome');
CALL CorteCor_AddPlanoContasColumn('Nivel', 'Nivel INT NULL AFTER IdPlanoPai');
CALL CorteCor_AddPlanoContasColumn('TipoConta', 'TipoConta VARCHAR(50) NULL AFTER Nivel');
CALL CorteCor_AddPlanoContasColumn('NaturezaSaldo', 'NaturezaSaldo VARCHAR(20) NULL AFTER TipoConta');
CALL CorteCor_AddPlanoContasColumn('AceitaLancamento', 'AceitaLancamento TINYINT(1) NOT NULL DEFAULT 1 AFTER NaturezaSaldo');
CALL CorteCor_AddPlanoContasColumn('GrupoDRE', 'GrupoDRE VARCHAR(100) NULL AFTER AceitaLancamento');
CALL CorteCor_AddPlanoContasColumn('OrdemDRE', 'OrdemDRE INT NULL AFTER GrupoDRE');
CALL CorteCor_AddPlanoContasIndex('IX_CorteCor_PlanoContas_Salao_Codigo', 'IX_CorteCor_PlanoContas_Salao_Codigo (IdSalao, Codigo)');
CALL CorteCor_AddPlanoContasIndex('IX_CorteCor_PlanoContas_Pai', 'IX_CorteCor_PlanoContas_Pai (IdSalao, IdPlanoPai, Ativo)');

DROP PROCEDURE IF EXISTS CorteCor_AddPlanoContasColumn;
DROP PROCEDURE IF EXISTS CorteCor_AddPlanoContasIndex;

DROP TEMPORARY TABLE IF EXISTS Tmp_PlanoContasContabil;
CREATE TEMPORARY TABLE Tmp_PlanoContasContabil (
    Codigo VARCHAR(30) NOT NULL PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL
);

INSERT INTO Tmp_PlanoContasContabil (Codigo, Nome) VALUES
('1', 'ATIVO'),
('1.1', 'ATIVO CIRCULANTE'),
('1.1.01', 'Caixa e Equivalentes de Caixa'),
('1.1.01.001', 'Caixa Geral'),
('1.1.01.002', 'Caixa Pequeno'),
('1.1.01.003', 'Banco Conta Corrente'),
('1.1.01.004', 'Banco Conta Poupanca'),
('1.1.01.005', 'Aplicacoes Financeiras de Liquidez Imediata'),
('1.1.01.006', 'Mercado Pago'),
('1.1.01.007', 'PagSeguro'),
('1.1.01.008', 'Outros Meios de Pagamento'),
('1.1.02', 'Clientes e Contas a Receber'),
('1.1.02.001', 'Clientes Nacionais'),
('1.1.02.002', 'Clientes Exterior'),
('1.1.02.003', 'Duplicatas a Receber'),
('1.1.02.004', 'Boletos a Receber'),
('1.1.02.005', 'Cartoes de Credito a Receber'),
('1.1.02.006', 'Cartoes de Debito a Receber'),
('1.1.02.007', 'PIX a Receber'),
('1.1.02.008', 'Cheques a Receber'),
('1.1.02.009', 'Provisao para Perdas com Clientes'),
('1.1.03', 'Estoques'),
('1.1.03.001', 'Mercadorias para Revenda'),
('1.1.03.002', 'Produtos Acabados'),
('1.1.03.003', 'Produtos em Elaboracao'),
('1.1.03.004', 'Materia-prima'),
('1.1.03.005', 'Materiais de Consumo'),
('1.1.03.006', 'Insumos'),
('1.1.03.007', 'Embalagens'),
('1.1.03.008', 'Provisao para Perdas em Estoque'),
('1.1.04', 'Tributos a Recuperar'),
('1.1.04.001', 'ICMS a Recuperar'),
('1.1.04.002', 'PIS a Recuperar'),
('1.1.04.003', 'COFINS a Recuperar'),
('1.1.04.004', 'IPI a Recuperar'),
('1.1.04.005', 'IRRF a Recuperar'),
('1.1.04.006', 'INSS a Recuperar'),
('1.1.04.007', 'Outros Tributos a Recuperar'),
('1.1.05', 'Adiantamentos'),
('1.1.05.001', 'Adiantamento a Fornecedores'),
('1.1.05.002', 'Adiantamento a Funcionarios'),
('1.1.05.003', 'Adiantamento de Viagens'),
('1.1.05.004', 'Adiantamento de Pro-labore'),
('1.1.05.005', 'Outros Adiantamentos'),
('1.1.06', 'Despesas Antecipadas'),
('1.1.06.001', 'Seguros a Apropriar'),
('1.1.06.002', 'Assinaturas a Apropriar'),
('1.1.06.003', 'Licencas de Software a Apropriar'),
('1.1.06.004', 'Alugueis Antecipados'),
('1.1.06.005', 'Outras Despesas Antecipadas'),
('1.2', 'ATIVO NAO CIRCULANTE'),
('1.2.01', 'Realizavel a Longo Prazo'),
('1.2.01.001', 'Clientes a Receber Longo Prazo'),
('1.2.01.002', 'Emprestimos a Socios'),
('1.2.01.003', 'Depositos Judiciais'),
('1.2.01.004', 'Caucoes e Garantias'),
('1.2.02', 'Investimentos'),
('1.2.02.001', 'Participacoes Societarias'),
('1.2.02.002', 'Investimentos Permanentes'),
('1.2.02.003', 'Imoveis para Investimento'),
('1.2.03', 'Imobilizado'),
('1.2.03.001', 'Moveis e Utensilios'),
('1.2.03.002', 'Maquinas e Equipamentos'),
('1.2.03.003', 'Computadores e Perifericos'),
('1.2.03.004', 'Veiculos'),
('1.2.03.005', 'Instalacoes'),
('1.2.03.006', 'Equipamentos de Comunicacao'),
('1.2.03.007', 'Equipamentos de Seguranca'),
('1.2.03.008', 'Imoveis'),
('1.2.03.099', 'Depreciacao Acumulada do Imobilizado'),
('1.2.04', 'Intangivel'),
('1.2.04.001', 'Softwares Adquiridos'),
('1.2.04.002', 'Sistemas Desenvolvidos'),
('1.2.04.003', 'Marcas e Patentes'),
('1.2.04.004', 'Dominios de Internet'),
('1.2.04.005', 'Direitos de Uso'),
('1.2.04.099', 'Amortizacao Acumulada do Intangivel'),
('2', 'PASSIVO'),
('2.1', 'PASSIVO CIRCULANTE'),
('2.1.01', 'Fornecedores'),
('2.1.01.001', 'Fornecedores Nacionais'),
('2.1.01.002', 'Fornecedores Exterior'),
('2.1.01.003', 'Prestadores de Servico a Pagar'),
('2.1.01.004', 'Compras Parceladas a Pagar'),
('2.1.02', 'Obrigacoes Trabalhistas'),
('2.1.02.001', 'Salarios a Pagar'),
('2.1.02.002', 'Pro-labore a Pagar'),
('2.1.02.003', 'Ferias a Pagar'),
('2.1.02.004', '13 Salario a Pagar'),
('2.1.02.005', 'Rescisoes a Pagar'),
('2.1.02.006', 'Comissoes a Pagar'),
('2.1.02.007', 'Beneficios a Pagar'),
('2.1.03', 'Obrigacoes Sociais'),
('2.1.03.001', 'INSS a Recolher'),
('2.1.03.002', 'FGTS a Recolher'),
('2.1.03.003', 'Contribuicao Sindical a Recolher'),
('2.1.03.004', 'Outras Obrigacoes Sociais'),
('2.1.04', 'Obrigacoes Tributarias'),
('2.1.04.001', 'Simples Nacional a Recolher'),
('2.1.04.002', 'ISS a Recolher'),
('2.1.04.003', 'ICMS a Recolher'),
('2.1.04.004', 'PIS a Recolher'),
('2.1.04.005', 'COFINS a Recolher'),
('2.1.04.006', 'IPI a Recolher'),
('2.1.04.007', 'IRPJ a Recolher'),
('2.1.04.008', 'CSLL a Recolher'),
('2.1.04.009', 'IRRF a Recolher'),
('2.1.04.010', 'Outros Tributos a Recolher'),
('2.1.05', 'Emprestimos e Financiamentos'),
('2.1.05.001', 'Emprestimos Bancarios Curto Prazo'),
('2.1.05.002', 'Financiamentos Curto Prazo'),
('2.1.05.003', 'Cartao de Credito Empresarial'),
('2.1.05.004', 'Cheque Especial'),
('2.1.05.005', 'Parcelamentos Tributarios Curto Prazo'),
('2.1.06', 'Adiantamentos de Clientes'),
('2.1.06.001', 'Adiantamento de Clientes'),
('2.1.06.002', 'Mensalidades Recebidas Antecipadamente'),
('2.1.06.003', 'Servicos Recebidos Antecipadamente'),
('2.2', 'PASSIVO NAO CIRCULANTE'),
('2.2.01', 'Emprestimos e Financiamentos Longo Prazo'),
('2.2.01.001', 'Emprestimos Bancarios Longo Prazo'),
('2.2.01.002', 'Financiamentos Longo Prazo'),
('2.2.01.003', 'Parcelamentos Tributarios Longo Prazo'),
('2.2.02', 'Provisoes'),
('2.2.02.001', 'Provisoes Trabalhistas'),
('2.2.02.002', 'Provisoes Tributarias'),
('2.2.02.003', 'Provisoes Civeis'),
('2.2.02.004', 'Outras Provisoes'),
('3', 'PATRIMONIO LIQUIDO'),
('3.1', 'Capital Social'),
('3.1.01', 'Capital Social Integralizado'),
('3.1.02', 'Capital Social a Integralizar'),
('3.2', 'Reservas'),
('3.2.01', 'Reservas de Lucros'),
('3.2.02', 'Reservas Legais'),
('3.2.03', 'Reservas Estatutarias'),
('3.3', 'Resultados Acumulados'),
('3.3.01', 'Lucros Acumulados'),
('3.3.02', 'Prejuizos Acumulados'),
('3.3.03', 'Resultado do Exercicio'),
('3.4', 'Distribuicoes e Retiradas'),
('3.4.01', 'Distribuicao de Lucros'),
('3.4.02', 'Retirada de Socios'),
('3.4.03', 'Dividendos a Pagar'),
('4', 'RECEITAS'),
('4.1', 'RECEITA BRUTA OPERACIONAL'),
('4.1.01', 'Receita de Venda de Mercadorias'),
('4.1.01.001', 'Venda de Mercadorias Nacionais'),
('4.1.01.002', 'Venda de Mercadorias para o Exterior'),
('4.1.01.003', 'Venda de Produtos de Fabricacao Propria'),
('4.1.02', 'Receita de Prestacao de Servicos'),
('4.1.02.001', 'Servicos Tecnicos'),
('4.1.02.002', 'Servicos Profissionais'),
('4.1.02.003', 'Consultoria'),
('4.1.02.004', 'Implantacao'),
('4.1.02.005', 'Suporte Tecnico'),
('4.1.02.006', 'Manutencao Mensal'),
('4.1.02.007', 'Treinamento'),
('4.1.02.008', 'Desenvolvimento de Sistemas'),
('4.1.02.009', 'Licenciamento de Software'),
('4.1.02.010', 'Assinaturas e Mensalidades'),
('4.1.02.011', 'Comissoes sobre Servicos'),
('4.1.02.012', 'Outras Receitas de Servicos'),
('4.1.03', 'Receita de Locacao'),
('4.1.03.001', 'Locacao de Equipamentos'),
('4.1.03.002', 'Locacao de Imoveis'),
('4.1.03.003', 'Locacao de Software ou Plataforma'),
('4.1.04', 'Receita de Intermediacao'),
('4.1.04.001', 'Comissao sobre Vendas'),
('4.1.04.002', 'Comissao sobre Contratos'),
('4.1.04.003', 'Comissao sobre Indicacoes'),
('5', 'DEDUCOES DA RECEITA BRUTA'),
('5.1', 'Cancelamentos, Devolucoes e Abatimentos'),
('5.1.01', 'Cancelamentos de Vendas'),
('5.1.01.001', 'Cancelamento de Venda de Mercadorias'),
('5.1.01.002', 'Cancelamento de Prestacao de Servicos'),
('5.1.02', 'Devolucoes de Vendas'),
('5.1.02.001', 'Devolucao de Mercadorias'),
('5.1.02.002', 'Devolucao de Produtos'),
('5.1.03', 'Abatimentos e Descontos Incondicionais'),
('5.1.03.001', 'Abatimentos Concedidos'),
('5.1.03.002', 'Descontos Comerciais Incondicionais'),
('5.1.03.003', 'Bonificacoes Concedidas'),
('5.2', 'Tributos sobre Vendas e Servicos'),
('5.2.01', 'ISS sobre Servicos'),
('5.2.02', 'ICMS sobre Vendas'),
('5.2.03', 'PIS sobre Receita'),
('5.2.04', 'COFINS sobre Receita'),
('5.2.05', 'IPI sobre Vendas'),
('5.2.06', 'Simples Nacional sobre Receita'),
('5.2.07', 'CPRB sobre Receita Bruta'),
('5.2.08', 'Outros Tributos sobre Receita'),
('6', 'CUSTOS'),
('6.1', 'CUSTO DAS MERCADORIAS VENDIDAS - CMV'),
('6.1.01', 'Custo de Mercadorias'),
('6.1.01.001', 'Custo das Mercadorias Vendidas'),
('6.1.01.002', 'Frete sobre Compras'),
('6.1.01.003', 'Seguro sobre Compras'),
('6.1.01.004', 'Perdas de Mercadorias'),
('6.1.01.005', 'Ajustes de Estoque'),
('6.1.01.006', 'Embalagens de Venda'),
('6.2', 'CUSTO DOS PRODUTOS VENDIDOS - CPV'),
('6.2.01', 'Materia-prima Consumida'),
('6.2.02', 'Mao de Obra Direta'),
('6.2.03', 'Encargos da Mao de Obra Direta'),
('6.2.04', 'Insumos de Producao'),
('6.2.05', 'Energia da Producao'),
('6.2.06', 'Manutencao da Producao'),
('6.2.07', 'Depreciacao da Producao'),
('6.2.08', 'Terceirizacao de Producao'),
('6.2.09', 'Perdas na Producao'),
('6.3', 'CUSTO DOS SERVICOS PRESTADOS - CSP'),
('6.3.01', 'Mao de Obra Direta dos Servicos'),
('6.3.02', 'Encargos da Mao de Obra Direta dos Servicos'),
('6.3.03', 'Prestadores Terceirizados Diretos'),
('6.3.04', 'Licencas Usadas na Entrega do Servico'),
('6.3.05', 'Servidores Usados na Entrega do Servico'),
('6.3.06', 'Hospedagem de Sistemas de Clientes'),
('6.3.07', 'APIs Pagas Usadas no Servico'),
('6.3.08', 'SMS, WhatsApp API e E-mail Transacional'),
('6.3.09', 'Deslocamento Tecnico para Atendimento'),
('6.3.10', 'Materiais Aplicados nos Servicos'),
('6.3.11', 'Suporte Direto ao Cliente'),
('6.3.12', 'Implantacao Direta de Sistemas'),
('6.3.13', 'Outros Custos Diretos dos Servicos'),
('7', 'DESPESAS OPERACIONAIS'),
('7.1', 'DESPESAS COMERCIAIS / VENDAS'),
('7.1.01', 'Marketing e Publicidade'),
('7.1.01.001', 'Google Ads'),
('7.1.01.002', 'Meta Ads'),
('7.1.01.003', 'Instagram'),
('7.1.01.004', 'Facebook'),
('7.1.01.005', 'LinkedIn Ads'),
('7.1.01.006', 'E-mail Marketing'),
('7.1.01.007', 'Agencia de Marketing'),
('7.1.01.008', 'Design e Criacao de Artes'),
('7.1.01.009', 'Producao de Videos'),
('7.1.01.010', 'Materiais Impressos'),
('7.1.01.011', 'Brindes e Material Promocional'),
('7.1.02', 'Vendas'),
('7.1.02.001', 'Comissao de Vendedores'),
('7.1.02.002', 'Comissao de Representantes'),
('7.1.02.003', 'Bonificacao Comercial'),
('7.1.02.004', 'Despesas com Propostas Comerciais'),
('7.1.02.005', 'Plataformas de Prospeccao'),
('7.1.02.006', 'CRM Comercial'),
('7.1.02.007', 'Visitas Comerciais'),
('7.1.02.008', 'Viagens Comerciais'),
('7.1.02.009', 'Alimentacao em Visitas Comerciais'),
('7.1.02.010', 'Hospedagem em Visitas Comerciais'),
('7.1.03', 'Pos-venda Comercial'),
('7.1.03.001', 'Relacionamento com Clientes'),
('7.1.03.002', 'Eventos Comerciais'),
('7.1.03.003', 'Feiras e Congressos'),
('7.2', 'DESPESAS ADMINISTRATIVAS'),
('7.2.01', 'Ocupacao e Estrutura'),
('7.2.01.001', 'Aluguel'),
('7.2.01.002', 'Condominio'),
('7.2.01.003', 'Energia Eletrica'),
('7.2.01.004', 'Agua e Esgoto'),
('7.2.01.005', 'Internet'),
('7.2.01.006', 'Telefone'),
('7.2.01.007', 'Limpeza e Conservacao'),
('7.2.01.008', 'Seguranca e Monitoramento'),
('7.2.01.009', 'Manutencao Predial'),
('7.2.01.010', 'IPTU'),
('7.2.01.011', 'Taxas Municipais'),
('7.2.02', 'Administracao Geral'),
('7.2.02.001', 'Material de Escritorio'),
('7.2.02.002', 'Material de Limpeza'),
('7.2.02.003', 'Correios'),
('7.2.02.004', 'Cartorio'),
('7.2.02.005', 'Certificado Digital'),
('7.2.02.006', 'Contabilidade'),
('7.2.02.007', 'Honorarios Advocaticios'),
('7.2.02.008', 'Consultorias Administrativas'),
('7.2.02.009', 'Associacoes e Entidades de Classe'),
('7.2.02.010', 'Taxas e Licencas Diversas'),
('7.2.03', 'Tecnologia Administrativa'),
('7.2.03.001', 'Hospedagem de Site Institucional'),
('7.2.03.002', 'Dominios de Internet'),
('7.2.03.003', 'E-mails Corporativos'),
('7.2.03.004', 'Softwares Administrativos'),
('7.2.03.005', 'Licencas de Sistemas Internos'),
('7.2.03.006', 'Antivirus e Seguranca Digital'),
('7.2.03.007', 'Manutencao de Computadores'),
('7.2.03.008', 'Equipamentos de Informatica de Pequeno Valor'),
('7.2.03.009', 'Backup e Armazenamento em Nuvem'),
('7.2.04', 'Depreciacao e Amortizacao Administrativa'),
('7.2.04.001', 'Depreciacao de Moveis e Utensilios'),
('7.2.04.002', 'Depreciacao de Computadores'),
('7.2.04.003', 'Depreciacao de Veiculos'),
('7.2.04.004', 'Depreciacao de Maquinas e Equipamentos'),
('7.2.04.005', 'Amortizacao de Softwares'),
('7.2.04.006', 'Amortizacao de Intangiveis'),
('7.3', 'DESPESAS COM PESSOAL'),
('7.3.01', 'Salarios e Remuneracoes'),
('7.3.01.001', 'Salarios'),
('7.3.01.002', 'Pro-labore'),
('7.3.01.003', 'Horas Extras'),
('7.3.01.004', 'Adicionais'),
('7.3.01.005', 'Comissoes Internas'),
('7.3.01.006', 'Bonificacoes'),
('7.3.01.007', '13 Salario'),
('7.3.01.008', 'Ferias'),
('7.3.01.009', 'Rescisoes'),
('7.3.02', 'Encargos Sociais'),
('7.3.02.001', 'INSS Patronal'),
('7.3.02.002', 'FGTS'),
('7.3.02.003', 'Multa FGTS'),
('7.3.02.004', 'Seguro Acidente de Trabalho'),
('7.3.02.005', 'Sistema S'),
('7.3.02.006', 'Outros Encargos sobre Folha'),
('7.3.03', 'Beneficios'),
('7.3.03.001', 'Vale-transporte'),
('7.3.03.002', 'Vale-alimentacao'),
('7.3.03.003', 'Vale-refeicao'),
('7.3.03.004', 'Plano de Saude'),
('7.3.03.005', 'Plano Odontologico'),
('7.3.03.006', 'Seguro de Vida'),
('7.3.03.007', 'Auxilio Home Office'),
('7.3.03.008', 'Outros Beneficios'),
('7.3.04', 'Treinamento e Desenvolvimento'),
('7.3.04.001', 'Cursos'),
('7.3.04.002', 'Certificacoes'),
('7.3.04.003', 'Treinamentos Internos'),
('7.3.04.004', 'Livros e Materiais Tecnicos'),
('7.3.04.005', 'Eventos e Congressos'),
('7.4', 'DESPESAS OPERACIONAIS GERAIS'),
('7.4.01', 'Veiculos e Deslocamentos'),
('7.4.01.001', 'Combustivel'),
('7.4.01.002', 'Estacionamento'),
('7.4.01.003', 'Pedagio'),
('7.4.01.004', 'Manutencao de Veiculos'),
('7.4.01.005', 'Seguro de Veiculos'),
('7.4.01.006', 'Aplicativos de Transporte'),
('7.4.01.007', 'Taxi'),
('7.4.01.008', 'Passagens'),
('7.4.02', 'Viagens'),
('7.4.02.001', 'Hospedagem'),
('7.4.02.002', 'Alimentacao em Viagem'),
('7.4.02.003', 'Passagens Aereas'),
('7.4.02.004', 'Passagens Rodoviarias'),
('7.4.02.005', 'Locacao de Veiculos'),
('7.4.02.006', 'Diarias de Viagem'),
('7.4.03', 'Servicos de Terceiros'),
('7.4.03.001', 'Servicos de Pessoa Juridica'),
('7.4.03.002', 'Servicos de Pessoa Fisica'),
('7.4.03.003', 'Freelancers'),
('7.4.03.004', 'Consultoria Tecnica'),
('7.4.03.005', 'Suporte Terceirizado'),
('7.4.03.006', 'Manutencao Terceirizada'),
('8', 'RESULTADO FINANCEIRO'),
('8.1', 'RECEITAS FINANCEIRAS'),
('8.1.01', 'Juros Recebidos'),
('8.1.02', 'Rendimentos de Aplicacoes Financeiras'),
('8.1.03', 'Descontos Obtidos'),
('8.1.04', 'Multas Recebidas'),
('8.1.05', 'Variacao Cambial Ativa'),
('8.1.06', 'Atualizacao Monetaria Ativa'),
('8.1.07', 'Outras Receitas Financeiras'),
('8.2', 'DESPESAS FINANCEIRAS'),
('8.2.01', 'Juros Pagos'),
('8.2.02', 'Multas Pagas'),
('8.2.03', 'Mora sobre Atrasos'),
('8.2.04', 'Tarifas Bancarias'),
('8.2.05', 'Taxas de Cartao de Credito'),
('8.2.06', 'Taxas de Cartao de Debito'),
('8.2.07', 'Taxas de Boleto'),
('8.2.08', 'Taxas de PIX'),
('8.2.09', 'Taxas do Mercado Pago'),
('8.2.10', 'Taxas do PagSeguro'),
('8.2.11', 'Taxas de Antecipacao de Recebiveis'),
('8.2.12', 'IOF'),
('8.2.13', 'Variacao Cambial Passiva'),
('8.2.14', 'Atualizacao Monetaria Passiva'),
('8.2.15', 'Descontos Concedidos Financeiros'),
('8.2.16', 'Outras Despesas Financeiras'),
('9', 'OUTRAS RECEITAS E DESPESAS'),
('9.1', 'OUTRAS RECEITAS OPERACIONAIS'),
('9.1.01', 'Venda de Ativo Imobilizado'),
('9.1.02', 'Ganho na Venda de Imobilizado'),
('9.1.03', 'Recuperacao de Despesas'),
('9.1.04', 'Recuperacao de Creditos Baixados'),
('9.1.05', 'Indenizacoes Recebidas'),
('9.1.06', 'Bonificacoes Recebidas'),
('9.1.07', 'Outras Receitas Operacionais'),
('9.2', 'OUTRAS DESPESAS OPERACIONAIS'),
('9.2.01', 'Perda na Venda de Imobilizado'),
('9.2.02', 'Baixa de Ativo Imobilizado'),
('9.2.03', 'Perdas Nao Recorrentes'),
('9.2.04', 'Indenizacoes Pagas'),
('9.2.05', 'Multas Administrativas'),
('9.2.06', 'Processos Judiciais'),
('9.2.07', 'Doacoes'),
('9.2.08', 'Outras Despesas Operacionais'),
('10', 'TRIBUTOS SOBRE O LUCRO E PARTICIPACOES'),
('10.1', 'TRIBUTOS SOBRE O LUCRO'),
('10.1.01', 'IRPJ'),
('10.1.01.001', 'IRPJ Corrente'),
('10.1.01.002', 'IRPJ Diferido'),
('10.1.02', 'CSLL'),
('10.1.02.001', 'CSLL Corrente'),
('10.1.02.002', 'CSLL Diferida'),
('10.2', 'PARTICIPACOES'),
('10.2.01', 'Participacao de Empregados'),
('10.2.02', 'Participacao de Administradores'),
('10.2.03', 'Participacao de Debenturistas'),
('10.2.04', 'Participacao de Partes Beneficiarias'),
('10.2.05', 'Outras Participacoes');

INSERT INTO CorteCor_PlanoContas
    (IdSalao, Codigo, Descricao, Tipo, Ativo, Nome, Nivel, TipoConta, NaturezaSaldo, AceitaLancamento, GrupoDRE, OrdemDRE)
SELECT
    S.IdSalao,
    T.Codigo,
    T.Nome,
    CASE WHEN T.Codigo REGEXP '^(4|8\\.1|9\\.1)' THEN 'R' ELSE 'D' END,
    1,
    T.Nome,
    1 + LENGTH(T.Codigo) - LENGTH(REPLACE(T.Codigo, '.', '')),
    CASE
        WHEN T.Codigo LIKE '10.1%' THEN 'Tributo sobre Lucro'
        WHEN T.Codigo LIKE '10.2%' THEN 'Participacao'
        WHEN T.Codigo = '10' THEN 'Tributo sobre Lucro'
        WHEN T.Codigo = '1' OR T.Codigo LIKE '1.%' THEN 'Ativo'
        WHEN T.Codigo = '2' OR T.Codigo LIKE '2.%' THEN 'Passivo'
        WHEN T.Codigo = '3' OR T.Codigo LIKE '3.%' THEN 'Patrimonio Liquido'
        WHEN T.Codigo = '4' OR T.Codigo LIKE '4.%' THEN 'Receita'
        WHEN T.Codigo = '5' OR T.Codigo LIKE '5.%' THEN 'Deducao da Receita'
        WHEN T.Codigo = '6' OR T.Codigo LIKE '6.%' THEN 'Custo'
        WHEN T.Codigo = '7' OR T.Codigo LIKE '7.%' THEN 'Despesa'
        WHEN T.Codigo LIKE '8.1%' THEN 'Receita Financeira'
        WHEN T.Codigo LIKE '8.2%' THEN 'Despesa Financeira'
        WHEN T.Codigo LIKE '9.1%' THEN 'Outras Receitas'
        WHEN T.Codigo LIKE '9.2%' THEN 'Outras Despesas'
        ELSE 'Despesa'
    END,
    CASE WHEN T.Codigo REGEXP '^(2|3|4|8\\.1|9\\.1)' THEN 'Credora' ELSE 'Devedora' END,
    1,
    CASE
        WHEN T.Codigo LIKE '4.1%' THEN 'Receita Bruta'
        WHEN T.Codigo LIKE '5.1%' OR T.Codigo LIKE '5.2%' THEN 'Deducoes da Receita'
        WHEN T.Codigo LIKE '6.1%' OR T.Codigo LIKE '6.2%' OR T.Codigo LIKE '6.3%' THEN 'Custos'
        WHEN T.Codigo LIKE '7.1%' THEN 'Despesas Comerciais'
        WHEN T.Codigo LIKE '7.2%' THEN 'Despesas Administrativas'
        WHEN T.Codigo LIKE '7.3%' THEN 'Despesas com Pessoal'
        WHEN T.Codigo LIKE '7.4%' THEN 'Despesas Operacionais Gerais'
        WHEN T.Codigo LIKE '8.1%' OR T.Codigo LIKE '8.2%' THEN 'Resultado Financeiro'
        WHEN T.Codigo LIKE '9.1%' THEN 'Outras Receitas Operacionais'
        WHEN T.Codigo LIKE '9.2%' THEN 'Outras Despesas Operacionais'
        WHEN T.Codigo LIKE '10.1%' THEN 'IRPJ e CSLL'
        WHEN T.Codigo LIKE '10.2%' THEN 'Participacoes'
        ELSE NULL
    END,
    CASE
        WHEN T.Codigo LIKE '4.1%' THEN 10
        WHEN T.Codigo LIKE '5.1%' OR T.Codigo LIKE '5.2%' THEN 20
        WHEN T.Codigo LIKE '6.1%' OR T.Codigo LIKE '6.2%' OR T.Codigo LIKE '6.3%' THEN 30
        WHEN T.Codigo LIKE '7.1%' THEN 40
        WHEN T.Codigo LIKE '7.2%' THEN 50
        WHEN T.Codigo LIKE '7.3%' THEN 60
        WHEN T.Codigo LIKE '7.4%' THEN 70
        WHEN T.Codigo LIKE '8.1%' OR T.Codigo LIKE '8.2%' THEN 80
        WHEN T.Codigo LIKE '9.1%' THEN 90
        WHEN T.Codigo LIKE '9.2%' THEN 100
        WHEN T.Codigo LIKE '10.1%' THEN 110
        WHEN T.Codigo LIKE '10.2%' THEN 120
        ELSE NULL
    END
FROM CorteCor_Salao S
CROSS JOIN Tmp_PlanoContasContabil T
WHERE NOT EXISTS (
    SELECT 1
    FROM CorteCor_PlanoContas P
    WHERE P.IdSalao = S.IdSalao
      AND P.Codigo = T.Codigo
);

UPDATE CorteCor_PlanoContas P
INNER JOIN Tmp_PlanoContasContabil T ON T.Codigo = P.Codigo
SET
    P.Descricao = T.Nome,
    P.Nome = T.Nome,
    P.Nivel = 1 + LENGTH(T.Codigo) - LENGTH(REPLACE(T.Codigo, '.', '')),
    P.Tipo = CASE WHEN T.Codigo REGEXP '^(4|8\\.1|9\\.1)' THEN 'R' ELSE 'D' END,
    P.TipoConta = CASE
        WHEN T.Codigo LIKE '10.1%' THEN 'Tributo sobre Lucro'
        WHEN T.Codigo LIKE '10.2%' THEN 'Participacao'
        WHEN T.Codigo = '10' THEN 'Tributo sobre Lucro'
        WHEN T.Codigo = '1' OR T.Codigo LIKE '1.%' THEN 'Ativo'
        WHEN T.Codigo = '2' OR T.Codigo LIKE '2.%' THEN 'Passivo'
        WHEN T.Codigo = '3' OR T.Codigo LIKE '3.%' THEN 'Patrimonio Liquido'
        WHEN T.Codigo = '4' OR T.Codigo LIKE '4.%' THEN 'Receita'
        WHEN T.Codigo = '5' OR T.Codigo LIKE '5.%' THEN 'Deducao da Receita'
        WHEN T.Codigo = '6' OR T.Codigo LIKE '6.%' THEN 'Custo'
        WHEN T.Codigo = '7' OR T.Codigo LIKE '7.%' THEN 'Despesa'
        WHEN T.Codigo LIKE '8.1%' THEN 'Receita Financeira'
        WHEN T.Codigo LIKE '8.2%' THEN 'Despesa Financeira'
        WHEN T.Codigo LIKE '9.1%' THEN 'Outras Receitas'
        WHEN T.Codigo LIKE '9.2%' THEN 'Outras Despesas'
        ELSE P.TipoConta
    END,
    P.NaturezaSaldo = CASE WHEN T.Codigo REGEXP '^(2|3|4|8\\.1|9\\.1)' THEN 'Credora' ELSE 'Devedora' END,
    P.GrupoDRE = CASE
        WHEN T.Codigo LIKE '4.1%' THEN 'Receita Bruta'
        WHEN T.Codigo LIKE '5.1%' OR T.Codigo LIKE '5.2%' THEN 'Deducoes da Receita'
        WHEN T.Codigo LIKE '6.1%' OR T.Codigo LIKE '6.2%' OR T.Codigo LIKE '6.3%' THEN 'Custos'
        WHEN T.Codigo LIKE '7.1%' THEN 'Despesas Comerciais'
        WHEN T.Codigo LIKE '7.2%' THEN 'Despesas Administrativas'
        WHEN T.Codigo LIKE '7.3%' THEN 'Despesas com Pessoal'
        WHEN T.Codigo LIKE '7.4%' THEN 'Despesas Operacionais Gerais'
        WHEN T.Codigo LIKE '8.1%' OR T.Codigo LIKE '8.2%' THEN 'Resultado Financeiro'
        WHEN T.Codigo LIKE '9.1%' THEN 'Outras Receitas Operacionais'
        WHEN T.Codigo LIKE '9.2%' THEN 'Outras Despesas Operacionais'
        WHEN T.Codigo LIKE '10.1%' THEN 'IRPJ e CSLL'
        WHEN T.Codigo LIKE '10.2%' THEN 'Participacoes'
        ELSE NULL
    END,
    P.OrdemDRE = CASE
        WHEN T.Codigo LIKE '4.1%' THEN 10
        WHEN T.Codigo LIKE '5.1%' OR T.Codigo LIKE '5.2%' THEN 20
        WHEN T.Codigo LIKE '6.1%' OR T.Codigo LIKE '6.2%' OR T.Codigo LIKE '6.3%' THEN 30
        WHEN T.Codigo LIKE '7.1%' THEN 40
        WHEN T.Codigo LIKE '7.2%' THEN 50
        WHEN T.Codigo LIKE '7.3%' THEN 60
        WHEN T.Codigo LIKE '7.4%' THEN 70
        WHEN T.Codigo LIKE '8.1%' OR T.Codigo LIKE '8.2%' THEN 80
        WHEN T.Codigo LIKE '9.1%' THEN 90
        WHEN T.Codigo LIKE '9.2%' THEN 100
        WHEN T.Codigo LIKE '10.1%' THEN 110
        WHEN T.Codigo LIKE '10.2%' THEN 120
        ELSE NULL
    END
WHERE P.IdPlano > 0;

UPDATE CorteCor_PlanoContas P
LEFT JOIN CorteCor_PlanoContas Pai
    ON Pai.IdSalao = P.IdSalao
   AND Pai.Codigo = CASE
        WHEN LOCATE('.', P.Codigo) = 0 THEN NULL
        ELSE SUBSTRING(P.Codigo, 1, LENGTH(P.Codigo) - LOCATE('.', REVERSE(P.Codigo)))
   END
SET P.IdPlanoPai = Pai.IdPlano
WHERE P.IdPlano > 0
  AND EXISTS (SELECT 1 FROM Tmp_PlanoContasContabil T WHERE T.Codigo = P.Codigo);

DROP TEMPORARY TABLE IF EXISTS Tmp_PlanoContasAceitaLancamento;
CREATE TEMPORARY TABLE Tmp_PlanoContasAceitaLancamento AS
SELECT
    P.IdPlano,
    CASE WHEN COUNT(Filho.IdPlano) > 0 THEN 0 ELSE 1 END AS AceitaLancamento
FROM CorteCor_PlanoContas P
INNER JOIN Tmp_PlanoContasContabil T ON T.Codigo = P.Codigo
LEFT JOIN CorteCor_PlanoContas Filho
    ON Filho.IdSalao = P.IdSalao
   AND Filho.Codigo LIKE CONCAT(P.Codigo, '.%')
WHERE P.IdPlano > 0
GROUP BY P.IdPlano;

ALTER TABLE Tmp_PlanoContasAceitaLancamento ADD PRIMARY KEY (IdPlano);

UPDATE CorteCor_PlanoContas P
INNER JOIN Tmp_PlanoContasAceitaLancamento A ON A.IdPlano = P.IdPlano
SET P.AceitaLancamento = A.AceitaLancamento
WHERE P.IdPlano > 0;

UPDATE CorteCor_PlanoContas
SET Nome = COALESCE(NULLIF(Nome, ''), Descricao),
    Nivel = COALESCE(NULLIF(Nivel, 0), 1 + LENGTH(Codigo) - LENGTH(REPLACE(Codigo, '.', ''))),
    AceitaLancamento = COALESCE(AceitaLancamento, 1)
WHERE IdPlano > 0
  AND Codigo IS NOT NULL;

DROP TEMPORARY TABLE IF EXISTS Tmp_PlanoContasAceitaLancamento;
DROP TEMPORARY TABLE IF EXISTS Tmp_PlanoContasContabil;

SELECT
    COUNT(*) AS TotalPlanoContas,
    SUM(CASE WHEN Nivel = 2 AND Ativo = 1 THEN 1 ELSE 0 END) AS TotalGruposNivel2,
    SUM(CASE WHEN AceitaLancamento = 1 AND Ativo = 1 THEN 1 ELSE 0 END) AS TotalContasAnaliticas
FROM CorteCor_PlanoContas;
