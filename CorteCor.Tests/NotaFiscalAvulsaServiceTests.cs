using CorteCor.Models;
using CorteCor.Services;
using Xunit;

namespace CorteCor.Tests
{
    public class NotaFiscalAvulsaServiceTests
    {
        [Theory]
        [InlineData("55", "NF-e")]
        [InlineData("65", "NFC-e")]
        [InlineData("NFSE", "NFS-e")]
        public void InferirTipoNota_DeveResolverModelo(string modelo, string esperado)
        {
            var tipo = NotaFiscalAvulsaService.InferirTipoNota(modelo);
            Assert.Equal(esperado, tipo);
        }

        [Fact]
        public void EhRetornoAutorizado_DeveReconhecerRetornoNfse()
        {
            const string xml = "<ret><chNFSe>123</chNFSe></ret>";
            Assert.True(NotaFiscalAvulsaService.EhRetornoAutorizado(xml));
        }

        [Fact]
        public void ExtrairMensagemRetorno_DeveCombinarCodigoEMotivo()
        {
            const string xml = "<ret><cStat>215</cStat><xMotivo>Falha de schema</xMotivo></ret>";
            var mensagem = NotaFiscalAvulsaService.ExtrairMensagemRetorno(xml);
            Assert.Equal("[215] Falha de schema", mensagem);
        }

        [Fact]
        public void ExtrairChaveNfse_DeveLerIdDaInfNfse()
        {
            const string xml = "<NFSe><infNFSe Id=\"NFS31433022249358717000107000000000000626030986125596\"><nNFSe>6</nNFSe></infNFSe></NFSe>";

            var chave = NotaFiscalAvulsaService.ExtrairChaveNfse(xml);

            Assert.Equal("31433022249358717000107000000000000626030986125596", chave);
        }

        [Theory]
        [InlineData("<ret><cStat>100</cStat></ret>", NotaFiscalStatus.Autorizada)]
        [InlineData("<ret><cStat>101</cStat></ret>", NotaFiscalStatus.Cancelada)]
        [InlineData("<ret><cStat>215</cStat></ret>", NotaFiscalStatus.Rejeitada)]
        public void ClassificarStatusPorXml_DeveClassificarRetorno(string xml, string esperado)
        {
            var status = NotaFiscalAvulsaService.ClassificarStatusPorXml(xml);
            Assert.Equal(esperado, status);
        }

        [Fact]
        public void CriarResumoRetorno_DevePreencherCamposPrincipais()
        {
            const string xml = "<ret><cStat>100</cStat><xMotivo>Autorizado o uso da NF-e</xMotivo><nProt>12345</nProt></ret>";

            var resumo = NotaFiscalAvulsaService.CriarResumoRetorno(xml, null, "CHAVE123");

            Assert.Equal(NotaFiscalStatus.Autorizada, resumo.StatusFiscal);
            Assert.Equal("100", resumo.CodigoStatus);
            Assert.Equal("[100] Autorizado o uso da NF-e", resumo.MensagemRetorno);
            Assert.Equal("12345", resumo.Protocolo);
            Assert.Equal("CHAVE123", resumo.ChaveAcesso);
            Assert.True(resumo.PodeCancelar);
            Assert.True(resumo.OperacaoConcluida);
        }

        [Fact]
        public void CriarResumoRetorno_DeveReconhecerCancelamento()
        {
            const string xml = "<ret><cStat>101</cStat><xMotivo>Cancelamento homologado</xMotivo><nProt>999</nProt></ret>";

            var resumo = NotaFiscalAvulsaService.CriarResumoRetorno(xml);

            Assert.Equal(NotaFiscalStatus.Cancelada, resumo.StatusFiscal);
            Assert.Equal("101", resumo.CodigoStatus);
            Assert.False(resumo.PodeCancelar);
            Assert.True(resumo.OperacaoConcluida);
        }

        [Theory]
        [InlineData("135", null, true)]
        [InlineData("Erro interno", "<ret><cStat>101</cStat></ret>", true)]
        [InlineData("Rejeicao", "<ret><cStat>215</cStat></ret>", false)]
        public void EhStatusEventoAutorizado_DeveClassificarEventos(string status, string? xmlRetorno, bool esperado)
        {
            var autorizado = NotaFiscalAvulsaService.EhStatusEventoAutorizado(status, xmlRetorno);
            Assert.Equal(esperado, autorizado);
        }

        [Theory]
        [InlineData("[E0840] O Sistema Nacional NFS-e nao pode recepcionar o EVENTO DE CANCELAMENTO DE NFS-e, pois o evento de Cancelamento de NFS-e ja esta vinculado a NFS-e indicada no evento enviado, impedindo sua recepcao.")]
        [InlineData(null, "<ret><cStat>E0840</cStat><xMotivo>O Sistema Nacional NFS-e ja esta vinculado ao evento.</xMotivo></ret>")]
        public void EhCancelamentoNfseJaVinculado_DeveReconhecerDuplicidadeDeEvento(string? status, string? xmlRetorno = null)
        {
            Assert.True(NotaFiscalAvulsaService.EhCancelamentoNfseJaVinculado(status, xmlRetorno));
        }

        [Fact]
        public void MontarMensagemOperacao_DeveUsarRetornoDoXmlQuandoStatusForGenerico()
        {
            const string xml = "<ret><cStat>215</cStat><xMotivo>Falha de schema</xMotivo></ret>";

            var mensagem = NotaFiscalAvulsaService.MontarMensagemOperacao("Cancelamento", "Vazio", xml);

            Assert.Equal("Cancelamento processada: [215] Falha de schema", mensagem);
        }

        [Theory]
        [InlineData("Vazio")]
        [InlineData("[E2406] A chave de acesso consultada deve conter 50 numeros.")]
        [InlineData("[E0840] O Sistema Nacional NFS-e nao pode recepcionar o EVENTO DE CANCELAMENTO DE NFS-e, pois o evento de Cancelamento de NFS-e ja esta vinculado a NFS-e indicada no evento enviado, impedindo sua recepcao.")]
        public void SanitizarProtocoloFiscal_DeveDescartarMensagensTecnicas(string protocolo)
        {
            var protocoloSanitizado = NotaFiscalAvulsaService.SanitizarProtocoloFiscal(protocolo);

            Assert.Null(protocoloSanitizado);
        }

        [Fact]
        public void SanitizarProtocoloFiscal_DeveManterValorValido()
        {
            var protocoloSanitizado = NotaFiscalAvulsaService.SanitizarProtocoloFiscal("3126");

            Assert.Equal("3126", protocoloSanitizado);
        }

        [Fact]
        public void ValidarFaixaInutilizacao_DeveAceitarFaixaValida()
        {
            NotaFiscalAvulsaService.ValidarFaixaInutilizacao(2026, 1, 10, 20, "NFC-e");
        }

        [Theory]
        [InlineData(1999, 1, 1, 2, "NF-e", "ano")]
        [InlineData(2026, 0, 1, 2, "NF-e", "serie")]
        [InlineData(2026, 1, 0, 2, "NF-e", "numeros")]
        [InlineData(2026, 1, 5, 4, "NF-e", "final")]
        [InlineData(2026, 1, 1, 2, "NFS-e", "suporta")]
        public void ValidarFaixaInutilizacao_DeveRejeitarFaixasInvalidas(int ano, int serie, int numInicial, int numFinal, string tipoNota, string trechoEsperado)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarFaixaInutilizacao(ano, serie, numInicial, numFinal, tipoNota));

            Assert.Contains(trechoEsperado, ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarCartaCorrecaoParaTipoNota_DeveBloquearNfse()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarCartaCorrecaoParaTipoNota("NFS-e"));

            Assert.Contains("NFS-e", ex.Message);
        }

        [Fact]
        public void ValidarCartaCorrecaoParaTipoNota_DevePermitirNotaEstadual()
        {
            NotaFiscalAvulsaService.ValidarCartaCorrecaoParaTipoNota("NFC-e");
        }

        [Fact]
        public void ValidarNotaParaCancelamento_DevePermitirNotaAutorizada()
        {
            NotaFiscalAvulsaService.ValidarNotaParaCancelamento(new NotaFiscal
            {
                Status = NotaFiscalStatus.Autorizada
            });
        }

        [Theory]
        [InlineData(NotaFiscalStatus.Cancelada, "cancelada")]
        [InlineData(NotaFiscalStatus.Inutilizada, "inutilizada")]
        [InlineData(NotaFiscalStatus.Rejeitada, "autorizadas")]
        public void ValidarNotaParaCancelamento_DeveRejeitarStatusInvalidos(string status, string trechoEsperado)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarNotaParaCancelamento(new NotaFiscal { Status = status }));

            Assert.Contains(trechoEsperado, ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarNotaParaCartaCorrecao_DevePermitirNotaAutorizada()
        {
            NotaFiscalAvulsaService.ValidarNotaParaCartaCorrecao(new NotaFiscal
            {
                Status = NotaFiscalStatus.Autorizada
            });
        }

        [Fact]
        public void ValidarNotaParaCartaCorrecao_DeveRejeitarNotaNaoAutorizada()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarNotaParaCartaCorrecao(new NotaFiscal
                {
                    Status = NotaFiscalStatus.Cancelada
                }));

            Assert.Contains("autorizada", ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ObterAcoesDisponiveis_DeveLiberarAcoesDeNotaAutorizadaEstadual()
        {
            var acoes = NotaFiscalAvulsaService.ObterAcoesDisponiveis(new NotaFiscal
            {
                TipoNota = "NFC-e",
                Status = NotaFiscalStatus.Autorizada,
                ChaveAcesso = "CHAVE123",
                XmlEnvio = "<xml/>"
            });

            Assert.Equal("CHAVE123", acoes.ChaveFiscal);
            Assert.Equal("bg-success", acoes.ClasseStatus);
            Assert.True(acoes.PodeBaixarXml);
            Assert.Equal("envio", acoes.TipoXmlPreferencial);
            Assert.True(acoes.PodeGerarPdf);
            Assert.True(acoes.PodeCancelar);
            Assert.True(acoes.PodeEnviarEmail);
            Assert.True(acoes.PodeCartaCorrecao);
        }

        [Fact]
        public void ObterAcoesDisponiveis_DeveAjustarAcoesDeNotaCancelada()
        {
            var acoes = NotaFiscalAvulsaService.ObterAcoesDisponiveis(new NotaFiscal
            {
                TipoNota = "NFS-e",
                Status = NotaFiscalStatus.Cancelada,
                XmlRetorno = "<NFSe><infNFSe Id=\"NFS31433022249358717000107000000000000626030986125596\"><nNFSe>6</nNFSe></infNFSe></NFSe>"
            });

            Assert.Equal("31433022249358717000107000000000000626030986125596", acoes.ChaveFiscal);
            Assert.Equal("bg-danger", acoes.ClasseStatus);
            Assert.True(acoes.PodeBaixarXml);
            Assert.Equal("retorno", acoes.TipoXmlPreferencial);
            Assert.True(acoes.PodeGerarPdf);
            Assert.False(acoes.PodeCancelar);
            Assert.True(acoes.PodeEnviarEmail);
            Assert.False(acoes.PodeCartaCorrecao);
        }

        [Fact]
        public void ObterAcoesDisponiveis_DevePreferirChaveNacionalDoXmlParaNfse()
        {
            var acoes = NotaFiscalAvulsaService.ObterAcoesDisponiveis(new NotaFiscal
            {
                TipoNota = "NFS-e",
                Status = NotaFiscalStatus.Autorizada,
                ChaveAcesso = "6",
                ChaveAcessoNacional = "3124",
                XmlRetorno = "<NFSe><infNFSe Id=\"NFS31433022249358717000107000000000000626030986125596\"><nNFSe>6</nNFSe></infNFSe></NFSe>"
            });

            Assert.Equal("31433022249358717000107000000000000626030986125596", acoes.ChaveFiscal);
            Assert.True(acoes.PodeCancelar);
        }

        [Fact]
        public void ValidarChaveFiscal_DeveRejeitarChaveVazia()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarChaveFiscal(" ", "consultar"));

            Assert.Contains("chave fiscal", ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarEmailDestino_DeveAceitarEmailValido()
        {
            NotaFiscalAvulsaService.ValidarEmailDestino("cliente@dominio.com");
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalido")]
        [InlineData("cliente@")]
        public void ValidarEmailDestino_DeveRejeitarEmailInvalido(string email)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarEmailDestino(email));

            Assert.Contains("e-mail", ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarTipoXmlSolicitado_DeveAceitarEnvioERetorno()
        {
            NotaFiscalAvulsaService.ValidarTipoXmlSolicitado("envio");
            NotaFiscalAvulsaService.ValidarTipoXmlSolicitado("retorno");
        }

        [Fact]
        public void ValidarTipoXmlSolicitado_DeveRejeitarTipoDesconhecido()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarTipoXmlSolicitado("autorizado"));

            Assert.Contains("XML", ex.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarTextoMinimo_DeveRejeitarTextoCurto()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NotaFiscalAvulsaService.ValidarTextoMinimo("curto", 10, "Texto invalido."));

            Assert.Equal("Texto invalido.", ex.Message);
        }

        [Fact]
        public async Task FiscalPdfGenerator_DeveGerarArquivoPdfValido()
        {
            var generator = new FiscalPdfGenerator();
            var bytes = await generator.GerarPdfAsync(new NotaFiscal
            {
                TipoNota = "NFS-e",
                Numero = 10,
                Serie = 1,
                Status = NotaFiscalStatus.Autorizada,
                ValorTotal = 150.75m,
                ProtocoloAutorizacao = "PROTO123",
                ChaveAcessoNacional = "NFSE123",
                XmlRetorno = "<xml />",
                DataEmissao = new DateTime(2026, 3, 16, 10, 30, 0)
            });

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 20);
            Assert.Equal("%PDF", System.Text.Encoding.UTF8.GetString(bytes, 0, 4));
        }
    }
}
