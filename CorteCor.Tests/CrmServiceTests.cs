using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace CorteCor.Tests
{
    public class CrmServiceTests
    {
        private readonly Mock<ICrmHandler> _crmHandler = new();
        private readonly Mock<PessoaHandler> _pessoaHandler = new((IDatabaseHandler)null);
        private readonly Mock<BrevoEmailService> _brevoEmailService;
        private readonly Mock<SMSMarketService> _smsMarketService;
        private readonly Mock<IWhatsappService> _whatsappService = new();
        private readonly Mock<FornecedoresHandler> _fornecedoresHandler;
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public CrmServiceTests()
        {
            var fakeDb = new Mock<IDatabaseHandler>();
            _fornecedoresHandler = new Mock<FornecedoresHandler>(fakeDb.Object);
            _brevoEmailService = new Mock<BrevoEmailService>(new HttpClient(), new Mock<ModeloEmailHandler>(fakeDb.Object).Object, _fornecedoresHandler.Object);
            _smsMarketService = new Mock<SMSMarketService>(new HttpClient(), _fornecedoresHandler.Object);
        }

        [Fact]
        public void RegistrarInteracao_DeveAtualizarUltimoContatoDoPerfil()
        {
            var perfil = new CrmPessoaPerfil { IdPerfil = 10, IdSalao = 1, IdPessoa = 7 };
            _crmHandler.Setup(h => h.ObterOuCriarPerfil(1, 7)).Returns(perfil);

            var service = CriarService();
            var agora = new DateTime(2026, 3, 23, 10, 0, 0);

            service.RegistrarInteracao(1, new CrmInteracao
            {
                IdPessoa = 7,
                Assunto = "Ligação de retorno",
                Canal = CrmCanal.Telefone,
                DataInteracao = agora
            });

            _crmHandler.Verify(h => h.AdicionarInteracao(It.Is<CrmInteracao>(i =>
                i.IdSalao == 1 &&
                i.IdPessoa == 7 &&
                i.Assunto == "Ligação de retorno")), Times.Once);
            _crmHandler.Verify(h => h.SalvarPerfil(It.Is<CrmPessoaPerfil>(p => p.UltimoContatoEm == agora)), Times.Once);
        }

        [Fact]
        public void SalvarTarefa_DeveEmpurrarProximaAcaoDoPerfil()
        {
            var perfil = new CrmPessoaPerfil { IdPerfil = 11, IdSalao = 1, IdPessoa = 9 };
            _crmHandler.Setup(h => h.ObterOuCriarPerfil(1, 9)).Returns(perfil);
            _crmHandler.Setup(h => h.SalvarTarefa(It.IsAny<CrmTarefa>())).Returns(55);

            var service = CriarService();
            var vencimento = new DateTime(2026, 3, 24, 14, 30, 0);

            var id = service.SalvarTarefa(1, new CrmTarefa
            {
                IdPessoa = 9,
                Titulo = "Retornar cliente",
                DataVencimento = vencimento
            });

            Assert.Equal(55, id);
            _crmHandler.Verify(h => h.SalvarPerfil(It.Is<CrmPessoaPerfil>(p => p.ProximaAcaoEm == vencimento)), Times.Once);
        }

        [Fact]
        public async Task EnviarCampanhaAsync_DeveRegistrarResultadoEAtualizarResumo()
        {
            var campanha = new CrmCampanha
            {
                IdCampanha = 14,
                IdSalao = 1,
                Nome = "Reativação",
                Canal = CrmCanal.Email,
                Segmento = CrmSegmentoCampanha.TodosClientes,
                Assunto = "Sentimos sua falta",
                Conteudo = "Olá {NomeCliente}"
            };

            _crmHandler.Setup(h => h.ObterCampanha(1, 14)).Returns(campanha);
            _crmHandler.Setup(h => h.ListarPublicoCampanha(1, campanha.Segmento, campanha.FiltroTag, campanha.DiasInatividade, campanha.IdPessoa))
                .Returns(new List<CrmContatoCampanha>
                {
                    new()
                    {
                        IdPessoa = 4,
                        IdSalao = 1,
                        Nome = "Maria",
                        Email = "maria@teste.com",
                        PermiteEmail = true
                    }
                });
            _crmHandler.Setup(h => h.ObterOuCriarPerfil(1, 4)).Returns(new CrmPessoaPerfil { IdPerfil = 1, IdSalao = 1, IdPessoa = 4 });
            _fornecedoresHandler.Setup(h => h.ObterEmailAtivo()).Returns(new FornecedorEmail { IdFornecedor = 1, Nome = "Brevo", ApiKey = "x", Endpoint = "https://api.brevo.com" });
            _brevoEmailService.Setup(s => s.EnviarEmailGenericoAsync("maria@teste.com", "Maria", campanha.Assunto!, It.IsAny<string>()))
                .ReturnsAsync((true, null));

            var service = CriarService();
            var resultado = await service.EnviarCampanhaAsync(1, 14, 99);

            Assert.Equal("Enviada", resultado.Campanha.Status);
            Assert.Single(resultado.Destinos);
            _crmHandler.Verify(h => h.RegistrarDestinoCampanha(It.Is<CrmCampanhaDestino>(d =>
                d.IdCampanha == 14 &&
                d.IdPessoa == 4 &&
                d.Status == "Sucesso")), Times.Once);
            _crmHandler.Verify(h => h.AtualizarResumoCampanha(1, 14, "Enviada", 1, 1, 0, It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task EnviarCampanhaAsync_Whatsapp_DeveUsarServicoDeWhatsapp()
        {
            var campanha = new CrmCampanha
            {
                IdCampanha = 18,
                IdSalao = 1,
                Nome = "Follow-up WhatsApp",
                Canal = CrmCanal.Whatsapp,
                Segmento = CrmSegmentoCampanha.TodosClientes,
                Conteudo = "Ola {NomeCliente}"
            };

            _crmHandler.Setup(h => h.ObterCampanha(1, 18)).Returns(campanha);
            _crmHandler.Setup(h => h.ListarPublicoCampanha(1, campanha.Segmento, campanha.FiltroTag, campanha.DiasInatividade, campanha.IdPessoa))
                .Returns(new List<CrmContatoCampanha>
                {
                    new()
                    {
                        IdPessoa = 9,
                        IdSalao = 1,
                        Nome = "Jose",
                        Telefone = "(31) 99999-8888",
                        PermiteWhatsapp = true
                    }
                });
            _crmHandler.Setup(h => h.ObterOuCriarPerfil(1, 9)).Returns(new CrmPessoaPerfil { IdPerfil = 2, IdSalao = 1, IdPessoa = 9 });
            _fornecedoresHandler.Setup(h => h.ObterWhatsappAtivo()).Returns(new FornecedorWhatsapp { IdFornecedor = 1, Nome = "Z-API", Endpoint = "https://api.exemplo.com", InstanceId = "abc", Token = "tok" });
            _whatsappService.Setup(s => s.EnviarMensagemAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((true, null));

            var service = CriarService();
            var resultado = await service.EnviarCampanhaAsync(1, 18, 99);

            Assert.Equal("Enviada", resultado.Campanha.Status);
            _whatsappService.Verify(s => s.EnviarMensagemAsync(It.Is<string>(t => t.Contains("99999-8888")), It.Is<string>(m => m.Contains("Jose"))), Times.Once);
        }

        private CrmService CriarService()
        {
            return new CrmService(
                _crmHandler.Object,
                _pessoaHandler.Object,
                _brevoEmailService.Object,
                _smsMarketService.Object,
                _whatsappService.Object,
                _fornecedoresHandler.Object,
                _cache);
        }
    }
}
