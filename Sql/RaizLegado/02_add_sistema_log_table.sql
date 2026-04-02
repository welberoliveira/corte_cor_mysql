-- Script: 02_add_sistema_log_table.sql
-- Descrição: Criação ou recriação da tabela CorteCor_NotaFiscalLog para auditoria de requests/responses e erros da emissão NFS-e Padrão Nacional.

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CorteCor_NotaFiscalLog]') AND type in (N'U'))
BEGIN
    DROP TABLE [dbo].[CorteCor_NotaFiscalLog];
    PRINT 'Tabela CorteCor_NotaFiscalLog existente foi removida.';
END
GO

CREATE TABLE [dbo].[CorteCor_NotaFiscalLog](
    [IdLog] [int] IDENTITY(1,1) NOT NULL,
    [IdSalao] [int] NOT NULL,
    [IdNotaFiscal] [uniqueidentifier] NULL,
    [IdAgendamento] [int] NULL,
    [DataHora] [datetime] NOT NULL DEFAULT GETDATE(),
    [TipoEvento] [varchar](255) NOT NULL, -- Ex: ErroEmissaoNfse, CancelamentoNfse, SucessoEmissao
    [RequestPayload] [nvarchar](max) NULL, -- JSON ou XML enviado
    [ResponsePayload] [nvarchar](max) NULL, -- JSON ou XML recebido
    [CodigoErro] [varchar](255) NULL,
    [Mensagem] [varchar](max) NULL,
    [Usuario] [varchar](100) NULL,
    CONSTRAINT [PK_CorteCor_NotaFiscalLog] PRIMARY KEY CLUSTERED 
    (
        [IdLog] ASC
    )
);

ALTER TABLE [dbo].[CorteCor_NotaFiscalLog] WITH CHECK ADD CONSTRAINT [FK_CorteCor_NotaFiscalLog_Salao] FOREIGN KEY([IdSalao])
REFERENCES [dbo].[CorteCor_Salao] ([IdSalao]);

ALTER TABLE [dbo].[CorteCor_NotaFiscalLog] CHECK CONSTRAINT [FK_CorteCor_NotaFiscalLog_Salao];

PRINT 'Nova tabela [CorteCor_NotaFiscalLog] criada com sucesso!';
GO
