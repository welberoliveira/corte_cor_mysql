-- Adicionando IdCategoria à tabela de Serviços para pareamento com Produtos
ALTER TABLE CorteCor_Servico
ADD IdCategoria INT NULL;

-- Criando chave estrangeira (opcional, mas recomendado)
-- ALTER TABLE CorteCor_Servico ADD CONSTRAINT FK_Servico_Categoria FOREIGN KEY (IdCategoria) REFERENCES CorteCor_CategoriaProduto(IdCategoria);
