CREATE TABLE CorteCor_ItemListaServico (
    IdItemListaServico INT IDENTITY(1,1) PRIMARY KEY,
    Codigo VARCHAR(10) NOT NULL,
    Descricao VARCHAR(500) NOT NULL
);

GO

-- Inserção de itens fundamentais para salões de beleza e estéticas com base na Lei Complementar 116/03
INSERT INTO CorteCor_ItemListaServico (Codigo, Descricao) VALUES 
('06.01', 'Barbearia, cabeleireiros, manicuros, pedicuros e congêneres.'),
('06.02', 'Esteticistas, tratamento de pele, depilação e congêneres.');

GO
