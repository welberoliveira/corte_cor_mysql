USE [CORTE_COR];
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CorteCor_NotaFiscalInutilizacao]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CorteCor_NotaFiscalInutilizacao](
        [IdInutilizacao] [uniqueidentifier] NOT NULL,
        [IdSalao] [int] NOT NULL,
        [Ano] [int] NOT NULL,
        [Serie] [int] NOT NULL,
        [NumeroInicial] [int] NOT NULL,
        [NumeroFinal] [int] NOT NULL,
        [TipoNota] [nvarchar](20) NOT NULL, -- NF-e ou NFC-e
        [Justificativa] [nvarchar](255) NOT NULL,
        [Protocolo] [nvarchar](100) NULL,
        [XmlEnvio] [nvarchar](max) NULL,
        [XmlRetorno] [nvarchar](max) NULL,
        [Status] [nvarchar](100) NULL,
        [DataInutilizacao] [datetime] NOT NULL,
        CONSTRAINT [PK_CorteCor_NotaFiscalInutilizacao] PRIMARY KEY CLUSTERED ([IdInutilizacao] ASC)
    );

    ALTER TABLE [dbo].[CorteCor_NotaFiscalInutilizacao] ADD CONSTRAINT [DF_CorteCor_NotaFiscalInutilizacao_IdInutilizacao] DEFAULT (newid()) FOR [IdInutilizacao];
    ALTER TABLE [dbo].[CorteCor_NotaFiscalInutilizacao] ADD CONSTRAINT [DF_CorteCor_NotaFiscalInutilizacao_DataInutilizacao] DEFAULT (getdate()) FOR [DataInutilizacao];
END
GO
