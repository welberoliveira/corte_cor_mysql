using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CorteCor.Services
{
    public class CrmService
    {
        private readonly ICrmHandler _crmHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly BrevoEmailService _brevoEmailService;
        private readonly SMSMarketService _smsMarketService;
        private readonly IWhatsappService _whatsappService;
        private readonly FornecedoresHandler _fornecedoresHandler;
        private readonly IMemoryCache _cache;

        public CrmService(
            ICrmHandler crmHandler,
            PessoaHandler pessoaHandler,
            BrevoEmailService brevoEmailService,
            SMSMarketService smsMarketService,
            IWhatsappService whatsappService,
            FornecedoresHandler fornecedoresHandler,
            IMemoryCache cache)
        {
            _crmHandler = crmHandler;
            _pessoaHandler = pessoaHandler;
            _brevoEmailService = brevoEmailService;
            _smsMarketService = smsMarketService;
            _whatsappService = whatsappService;
            _fornecedoresHandler = fornecedoresHandler;
            _cache = cache;
        }

        public void GarantirEstrutura(int idSalao)
        {
            var cacheKey = $"crm:etapas-padrao:{idSalao}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                return;
            }

            _crmHandler.GarantirEtapasPadrao(idSalao);
            _cache.Set(cacheKey, true, TimeSpan.FromMinutes(15));
        }

        public CrmDashboardResumo ObterDashboard(int idSalao)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ObterDashboard(idSalao);
        }

        public CrmRelatorioResumo ObterRelatorios(int idSalao, DateTime dataInicio, DateTime dataFim, int? idUsuarioResponsavel = null)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ObterRelatorios(idSalao, dataInicio, dataFim, idUsuarioResponsavel);
        }

        public List<Usuario> ListarResponsaveis(int idSalao)
        {
            return _crmHandler.ListarResponsaveis(idSalao);
        }

        public PagedResult<CrmClienteResumo> ListarClientes(int idSalao, string? pesquisa, int pageIndex, int pageSize)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ListarClientesResumo(idSalao, pesquisa, pageIndex, pageSize);
        }

        public CrmClienteDetalhe ObterClienteDetalhe(int idSalao, int idPessoa)
        {
            GarantirEstrutura(idSalao);
            var pessoa = _pessoaHandler.ObterPorIdESalao(idPessoa, idSalao)
                ?? throw new InvalidOperationException("Cliente não encontrado para o CRM.");

            return new CrmClienteDetalhe
            {
                Pessoa = pessoa,
                Perfil = _crmHandler.ObterOuCriarPerfil(idSalao, idPessoa),
                Resumo = _crmHandler.ObterClienteResumo(idSalao, idPessoa),
                Timeline = _crmHandler.ListarTimeline(idSalao, idPessoa, 80),
                TarefasAbertas = _crmHandler.ListarTarefas(idSalao, idPessoa, CrmStatusTarefa.Aberta, null, 1, 10).Items,
                OportunidadesAbertas = _crmHandler.ListarOportunidades(idSalao, idPessoa, null)
                    .Where(o => o.Status == CrmStatusOportunidade.Aberta)
                    .ToList()
            };
        }

        public List<CrmInteracao> ListarInteracoes(int idSalao, int idPessoa, int limit = 50)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ListarInteracoes(idSalao, idPessoa, limit);
        }

        public CrmPessoaPerfil SalvarPerfil(int idSalao, CrmPessoaPerfil perfil)
        {
            var existente = _crmHandler.ObterOuCriarPerfil(idSalao, perfil.IdPessoa);
            existente.StatusRelacionamento = string.IsNullOrWhiteSpace(perfil.StatusRelacionamento) ? existente.StatusRelacionamento : perfil.StatusRelacionamento.Trim();
            existente.OrigemLead = perfil.OrigemLead?.Trim();
            existente.Temperatura = string.IsNullOrWhiteSpace(perfil.Temperatura) ? existente.Temperatura : perfil.Temperatura.Trim();
            existente.ScoreRelacionamento = perfil.ScoreRelacionamento;
            existente.PermiteEmail = perfil.PermiteEmail;
            existente.PermiteSms = perfil.PermiteSms;
            existente.PermiteWhatsapp = perfil.PermiteWhatsapp;
            existente.NaoPerturbe = perfil.NaoPerturbe;
            existente.ProximaAcaoEm = perfil.ProximaAcaoEm;
            existente.ObservacoesInternas = perfil.ObservacoesInternas?.Trim();
            _crmHandler.SalvarPerfil(existente);
            return existente;
        }

        public void RegistrarInteracao(int idSalao, CrmInteracao interacao)
        {
            interacao.IdSalao = idSalao;
            interacao.Assunto = interacao.Assunto?.Trim() ?? string.Empty;
            interacao.Descricao = interacao.Descricao?.Trim();
            interacao.Canal = string.IsNullOrWhiteSpace(interacao.Canal) ? CrmCanal.Sistema : interacao.Canal.Trim();
            interacao.Tipo = string.IsNullOrWhiteSpace(interacao.Tipo) ? "Manual" : interacao.Tipo.Trim();
            interacao.DataInteracao = interacao.DataInteracao == default ? DateTime.Now : interacao.DataInteracao;

            if (interacao.IdPessoa <= 0)
            {
                throw new InvalidOperationException("Selecione um cliente para registrar a interação.");
            }

            if (string.IsNullOrWhiteSpace(interacao.Assunto))
            {
                throw new InvalidOperationException("Informe um assunto para a interação.");
            }

            _crmHandler.AdicionarInteracao(interacao);

            var perfil = _crmHandler.ObterOuCriarPerfil(idSalao, interacao.IdPessoa);
            perfil.UltimoContatoEm = interacao.DataInteracao;
            _crmHandler.SalvarPerfil(perfil);
        }

        public int SalvarInteracao(int idSalao, CrmInteracao interacao)
        {
            interacao.IdSalao = idSalao;
            interacao.Assunto = interacao.Assunto?.Trim() ?? string.Empty;
            interacao.Descricao = interacao.Descricao?.Trim();
            interacao.Canal = string.IsNullOrWhiteSpace(interacao.Canal) ? CrmCanal.Sistema : interacao.Canal.Trim();
            interacao.Tipo = string.IsNullOrWhiteSpace(interacao.Tipo) ? "Manual" : interacao.Tipo.Trim();
            interacao.DataInteracao = interacao.DataInteracao == default ? DateTime.Now : interacao.DataInteracao;

            if (interacao.IdPessoa <= 0)
            {
                throw new InvalidOperationException("Selecione um cliente para registrar a interação.");
            }

            if (string.IsNullOrWhiteSpace(interacao.Assunto))
            {
                throw new InvalidOperationException("Informe um assunto para a interação.");
            }

            var idInteracao = _crmHandler.SalvarInteracao(interacao);

            var perfil = _crmHandler.ObterOuCriarPerfil(idSalao, interacao.IdPessoa);
            perfil.UltimoContatoEm = interacao.DataInteracao;
            _crmHandler.SalvarPerfil(perfil);

            return idInteracao;
        }

        public PagedResult<CrmTarefa> ListarTarefas(int idSalao, int? idPessoa, string? status, int? idUsuarioResponsavel, int pageIndex, int pageSize, string? pesquisa = null, DateTime? dataVencimentoInicio = null, DateTime? dataVencimentoFim = null)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ListarTarefas(idSalao, idPessoa, status, idUsuarioResponsavel, pageIndex, pageSize, pesquisa, dataVencimentoInicio, dataVencimentoFim);
        }

        public CrmTarefa? ObterTarefa(int idSalao, int idTarefa)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ObterTarefa(idSalao, idTarefa);
        }

        public int SalvarTarefa(int idSalao, CrmTarefa tarefa)
        {
            tarefa.IdSalao = idSalao;
            tarefa.Titulo = tarefa.Titulo?.Trim() ?? string.Empty;
            tarefa.Descricao = tarefa.Descricao?.Trim();
            tarefa.Prioridade = string.IsNullOrWhiteSpace(tarefa.Prioridade) ? "Media" : tarefa.Prioridade.Trim();
            tarefa.Status = string.IsNullOrWhiteSpace(tarefa.Status) ? CrmStatusTarefa.Aberta : tarefa.Status.Trim();

            if (string.IsNullOrWhiteSpace(tarefa.Titulo))
            {
                throw new InvalidOperationException("Informe o título da tarefa.");
            }

            if (tarefa.DataVencimento == default)
            {
                throw new InvalidOperationException("Informe a data de vencimento da tarefa.");
            }

            var id = _crmHandler.SalvarTarefa(tarefa);
            if (tarefa.IdPessoa.HasValue && tarefa.IdPessoa.Value > 0)
            {
                var perfil = _crmHandler.ObterOuCriarPerfil(idSalao, tarefa.IdPessoa.Value);
                perfil.ProximaAcaoEm = tarefa.DataVencimento;
                _crmHandler.SalvarPerfil(perfil);
            }

            return id;
        }

        public void AtualizarStatusTarefa(int idSalao, int idTarefa, string status)
        {
            var statusFinal = string.IsNullOrWhiteSpace(status) ? CrmStatusTarefa.Aberta : status.Trim();
            _crmHandler.AtualizarStatusTarefa(
                idSalao,
                idTarefa,
                statusFinal,
                statusFinal == CrmStatusTarefa.Concluida ? DateTime.Now : null);
        }

        public List<CrmEtapaFunil> ListarEtapas(int idSalao)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ListarEtapas(idSalao);
        }

        public List<CrmOportunidade> ListarOportunidades(int idSalao, int? idPessoa, string? status)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ListarOportunidades(idSalao, idPessoa, status);
        }

        public PagedResult<CrmOportunidade> ListarOportunidadesPaginadas(int idSalao, int? idPessoa, string? status, DateTime? dataInicio, DateTime? dataFim, int pageIndex, int pageSize)
        {
            GarantirEstrutura(idSalao);
            return _crmHandler.ListarOportunidadesPaginadas(idSalao, idPessoa, status, dataInicio, dataFim, pageIndex, pageSize);
        }

        public int SalvarOportunidade(int idSalao, CrmOportunidade oportunidade)
        {
            GarantirEstrutura(idSalao);
            oportunidade.IdSalao = idSalao;
            oportunidade.Titulo = oportunidade.Titulo?.Trim() ?? string.Empty;
            oportunidade.Descricao = oportunidade.Descricao?.Trim();
            oportunidade.Origem = oportunidade.Origem?.Trim();
            oportunidade.Status = string.IsNullOrWhiteSpace(oportunidade.Status) ? CrmStatusOportunidade.Aberta : oportunidade.Status.Trim();
            oportunidade.Probabilidade = Math.Clamp(oportunidade.Probabilidade, 0, 100);

            if (oportunidade.IdPessoa <= 0)
            {
                throw new InvalidOperationException("Selecione um cliente para a oportunidade.");
            }

            if (oportunidade.IdEtapa <= 0)
            {
                throw new InvalidOperationException("Selecione uma etapa do funil.");
            }

            if (string.IsNullOrWhiteSpace(oportunidade.Titulo))
            {
                throw new InvalidOperationException("Informe o título da oportunidade.");
            }

            return _crmHandler.SalvarOportunidade(oportunidade);
        }

        public void MoverOportunidade(int idSalao, int idOportunidade, int idEtapa)
        {
            GarantirEstrutura(idSalao);
            var etapa = _crmHandler.ListarEtapas(idSalao).FirstOrDefault(e => e.IdEtapa == idEtapa)
                ?? throw new InvalidOperationException("Etapa do funil não encontrada.");

            var status = etapa.Ganha ? CrmStatusOportunidade.Ganha : etapa.Perdida ? CrmStatusOportunidade.Perdida : CrmStatusOportunidade.Aberta;
            _crmHandler.AtualizarEtapaOportunidade(idSalao, idOportunidade, idEtapa, status, status == CrmStatusOportunidade.Aberta ? null : DateTime.Now);
        }

        public PagedResult<CrmCampanha> ListarCampanhas(int idSalao, int pageIndex, int pageSize, string? pesquisa = null, string? canal = null, string? segmento = null, string? status = null)
        {
            return _crmHandler.ListarCampanhas(idSalao, pageIndex, pageSize, pesquisa, canal, segmento, status);
        }

        public CrmCampanha? ObterCampanha(int idSalao, int idCampanha)
        {
            return _crmHandler.ObterCampanha(idSalao, idCampanha);
        }

        public List<CrmCampanhaDestino> ListarDestinosCampanha(int idSalao, int idCampanha, int limit = 100)
        {
            return _crmHandler.ListarDestinosCampanha(idSalao, idCampanha, limit);
        }

        public int SalvarCampanha(int idSalao, CrmCampanha campanha)
        {
            campanha.IdSalao = idSalao;
            campanha.Nome = campanha.Nome?.Trim() ?? string.Empty;
            campanha.Canal = string.IsNullOrWhiteSpace(campanha.Canal) ? CrmCanal.Email : campanha.Canal.Trim();
            campanha.Segmento = string.IsNullOrWhiteSpace(campanha.Segmento) ? CrmSegmentoCampanha.TodosClientes : campanha.Segmento.Trim();
            campanha.FiltroTag = campanha.FiltroTag?.Trim();
            campanha.Assunto = campanha.Assunto?.Trim();
            campanha.Conteudo = campanha.Conteudo?.Trim() ?? string.Empty;
            campanha.Status = string.IsNullOrWhiteSpace(campanha.Status) ? "Rascunho" : campanha.Status.Trim();

            if (string.IsNullOrWhiteSpace(campanha.Nome))
            {
                throw new InvalidOperationException("Informe o nome da campanha.");
            }

            if (string.IsNullOrWhiteSpace(campanha.Conteudo))
            {
                throw new InvalidOperationException("Informe o conteúdo da campanha.");
            }

            if (campanha.Canal == CrmCanal.Email && string.IsNullOrWhiteSpace(campanha.Assunto))
            {
                throw new InvalidOperationException("Campanhas por e-mail exigem um assunto.");
            }

            return _crmHandler.SalvarCampanha(campanha);
        }

        public async Task<(CrmCampanha Campanha, List<CrmCampanhaDestino> Destinos)> EnviarCampanhaAsync(int idSalao, int idCampanha, int? idUsuario)
        {
            var campanha = _crmHandler.ObterCampanha(idSalao, idCampanha)
                ?? throw new InvalidOperationException("Campanha não encontrada.");

            if (campanha.Canal == CrmCanal.Email && _fornecedoresHandler.ObterEmailAtivo() == null)
            {
                throw new InvalidOperationException("Não existe fornecedor de e-mail ativo para o CRM.");
            }

            if (campanha.Canal == CrmCanal.Sms && _fornecedoresHandler.ObterSMSAtivo() == null)
            {
                throw new InvalidOperationException("Não existe fornecedor de SMS ativo para o CRM.");
            }

            if (campanha.Canal == CrmCanal.Whatsapp && _fornecedoresHandler.ObterWhatsappAtivo() == null)
            {
                throw new InvalidOperationException("Não existe fornecedor de WhatsApp ativo para o CRM.");
            }

            var publico = _crmHandler.ListarPublicoCampanha(
                idSalao,
                campanha.Segmento,
                campanha.FiltroTag,
                campanha.DiasInatividade,
                campanha.IdPessoa);

            var destinos = new List<CrmCampanhaDestino>();
            foreach (var contato in publico)
            {
                if (contato.NaoPerturbe)
                {
                    continue;
                }

                var destino = await EnviarParaContatoAsync(campanha, contato);
                destinos.Add(destino);
                _crmHandler.RegistrarDestinoCampanha(destino);

                var interacaoCampanha = new CrmInteracao
                {
                    IdPessoa = contato.IdPessoa,
                    IdUsuario = idUsuario,
                    Canal = campanha.Canal,
                    Tipo = "Campanha",
                    Assunto = campanha.Nome,
                    Descricao = destino.Status == "Sucesso"
                        ? $"Campanha enviada com sucesso para {destino.Destino}."
                        : $"Falha ao enviar campanha para {destino.Destino}: {destino.MensagemErro}",
                    Referencia = $"Campanha:{campanha.IdCampanha}",
                    DataInteracao = destino.DataEnvio,
                    OrigemSistema = true
                };

                if (destino.Status == "Sucesso")
                {
                    RegistrarInteracao(idSalao, interacaoCampanha);
                }
                else
                {
                    interacaoCampanha.IdSalao = idSalao;
                    _crmHandler.AdicionarInteracao(interacaoCampanha);
                }
            }

            var sucesso = destinos.Count(d => d.Status == "Sucesso");
            var falha = destinos.Count(d => d.Status != "Sucesso");
            campanha.Status = falha == 0 ? "Enviada" : sucesso > 0 ? "Parcial" : "Erro";
            campanha.TotalDestinatarios = destinos.Count;
            campanha.TotalSucesso = sucesso;
            campanha.TotalFalha = falha;
            campanha.UltimoEnvioEm = DateTime.Now;
            _crmHandler.AtualizarResumoCampanha(idSalao, campanha.IdCampanha, campanha.Status, campanha.TotalDestinatarios, campanha.TotalSucesso, campanha.TotalFalha, campanha.UltimoEnvioEm);

            return (campanha, destinos);
        }

        private async Task<CrmCampanhaDestino> EnviarParaContatoAsync(CrmCampanha campanha, CrmContatoCampanha contato)
        {
            var destino = new CrmCampanhaDestino
            {
                IdCampanha = campanha.IdCampanha,
                IdSalao = campanha.IdSalao,
                IdPessoa = contato.IdPessoa,
                Canal = campanha.Canal,
                DataEnvio = DateTime.Now
            };

            if (campanha.Canal == CrmCanal.Email)
            {
                if (!contato.PermiteEmail || string.IsNullOrWhiteSpace(contato.Email))
                {
                    destino.Destino = contato.Email ?? string.Empty;
                    destino.Status = "Ignorado";
                    destino.MensagemErro = "Cliente sem permissão ou sem e-mail para campanha.";
                    return destino;
                }

                destino.Destino = contato.Email!;
                var (ok, erro) = await _brevoEmailService.EnviarEmailGenericoAsync(
                    contato.Email!,
                    contato.Nome,
                    campanha.Assunto ?? campanha.Nome,
                    InterpolarConteudo(campanha.Conteudo, contato));
                destino.Status = ok ? "Sucesso" : "Erro";
                destino.MensagemErro = erro;
                return destino;
            }

            if (campanha.Canal == CrmCanal.Sms)
            {
                if (!contato.PermiteSms || string.IsNullOrWhiteSpace(contato.Telefone))
                {
                    destino.Destino = contato.Telefone ?? string.Empty;
                    destino.Status = "Ignorado";
                    destino.MensagemErro = "Cliente sem permissão ou sem telefone para campanha.";
                    return destino;
                }

                destino.Destino = contato.Telefone!;
                var fornecedorSms = _fornecedoresHandler.ObterSMSAtivo();
                var conteudoSms = RemoverHtml(InterpolarConteudo(campanha.Conteudo, contato));
                (bool ok, string? erro) = fornecedorSms != null && fornecedorSms.Nome.Equals("Brevo", StringComparison.OrdinalIgnoreCase)
                    ? await _brevoEmailService.EnviarSmsAsync(contato.Telefone!, conteudoSms)
                    : await _smsMarketService.EnviarSmsAsync(contato.Telefone!, conteudoSms);
                destino.Status = ok ? "Sucesso" : "Erro";
                destino.MensagemErro = erro;
                return destino;
            }

            if (campanha.Canal == CrmCanal.Whatsapp)
            {
                if (!contato.PermiteWhatsapp || string.IsNullOrWhiteSpace(contato.Telefone))
                {
                    destino.Destino = contato.Telefone ?? string.Empty;
                    destino.Status = "Ignorado";
                    destino.MensagemErro = "Cliente sem permissão ou sem telefone para campanha.";
                    return destino;
                }

                destino.Destino = contato.Telefone!;
                var conteudoWhatsapp = RemoverHtml(InterpolarConteudo(campanha.Conteudo, contato));
                var (ok, erro) = await _whatsappService.EnviarMensagemAsync(contato.Telefone!, conteudoWhatsapp);
                destino.Status = ok ? "Sucesso" : "Erro";
                destino.MensagemErro = erro;
                return destino;
            }

            destino.Destino = contato.Telefone ?? contato.Email ?? string.Empty;
            destino.Status = "Ignorado";
            destino.MensagemErro = "Canal ainda não suportado para envio automático.";
            return destino;
        }

        private static string InterpolarConteudo(string conteudo, CrmContatoCampanha contato)
        {
            return (conteudo ?? string.Empty)
                .Replace("{NomeCliente}", contato.Nome ?? string.Empty)
                .Replace("{Email}", contato.Email ?? string.Empty)
                .Replace("{Telefone}", contato.Telefone ?? string.Empty)
                .Replace("{Tags}", contato.Tags ?? string.Empty)
                .Replace("{UltimoAgendamento}", contato.UltimoAgendamentoEm?.ToString("dd/MM/yyyy") ?? string.Empty)
                .Replace("{DataNascimento}", contato.DataNascimento?.ToString("dd/MM") ?? string.Empty);
        }

        private static string RemoverHtml(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var semTags = System.Text.RegularExpressions.Regex.Replace(texto, "<.*?>", " ");
            return System.Net.WebUtility.HtmlDecode(semTags).Trim();
        }
    }
}

