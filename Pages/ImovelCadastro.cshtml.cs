using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages;

[Authorize(Policy = "UsuarioPolicy")]
[RequestSizeLimit(314572800)]
[RequestFormLimits(MultipartBodyLengthLimit = 314572800, ValueLengthLimit = 10485760, MultipartHeadersLengthLimit = 65536, ValueCountLimit = 8192)]
public class ImovelCadastroModel : PageModel
{
    private readonly ImovelHandler _imovelHandler;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImovelCadastroModel> _logger;

    [BindProperty]
    public Imovel Imovel { get; set; } = new();

    public string Mensagem { get; set; } = string.Empty;
    public string MensagemDetalheTecnico { get; set; } = string.Empty;
    public string MensagemTipo { get; set; } = "success";
    public string ButtonText { get; set; } = "Cadastrar";

    public IReadOnlyList<string> StatusOptions { get; } = new[] { "Ativo", "Inativo", "Vendido", "Alugado", "Rascunho" };
    public IReadOnlyList<string> FinalidadeOptions { get; } = new[] { "Venda", "Aluguel", "Temporada" };
    public IReadOnlyList<string> TipoOptions { get; } = new[] { "Casa", "Apartamento", "Lote", "Sala", "Cobertura", "Terreno", "Comercial", "Rural", "Outro" };
    public IReadOnlyList<string> OrigemOptions { get; } = new[] { "Manual", "Importacao", "CRM", "Portal", "Outro" };
    public IReadOnlyList<string> EstadoOptions { get; } = new[]
    {
        "Acre", "Alagoas", "Amapa", "Amazonas", "Bahia", "Ceara", "Distrito Federal", "Espirito Santo",
        "Goias", "Maranhao", "Mato Grosso", "Mato Grosso do Sul", "Minas Gerais", "Para", "Paraiba",
        "Parana", "Pernambuco", "Piaui", "Rio de Janeiro", "Rio Grande do Norte", "Rio Grande do Sul",
        "Rondonia", "Roraima", "Santa Catarina", "Sao Paulo", "Sergipe", "Tocantins"
    };

    public ImovelCadastroModel(ImovelHandler imovelHandler, IWebHostEnvironment environment, ILogger<ImovelCadastroModel> logger)
    {
        _imovelHandler = imovelHandler;
        _environment = environment;
        _logger = logger;
    }

    public void OnGet(int? id)
    {
        if (!TryObterIdSalao(out var idSalao))
        {
            Mensagem = "Nao foi possivel identificar a empresa atual.";
            MensagemTipo = "danger";
            Imovel = CriarNovoImovel();
            return;
        }

        if (id.HasValue && id > 0)
        {
            Imovel = _imovelHandler.ObterPorIdESalao(id.Value, idSalao) ?? CriarNovoImovel();
            ButtonText = Imovel.IdImovel > 0 ? "Atualizar" : "Cadastrar";
            return;
        }

        Imovel = CriarNovoImovel();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!TryObterIdSalao(out var idSalao))
        {
            Mensagem = "Nao foi possivel identificar a empresa atual.";
            MensagemTipo = "danger";
            return Page();
        }

        var idAtual = ParseNullableInt(Request.Form["idImovel"]) ?? 0;
        var fotosExistentes = idAtual > 0 ? _imovelHandler.ListarFotos(idAtual) : new List<ImovelFoto>();
        var leadsExistentes = idAtual > 0 ? _imovelHandler.ListarLeads(idAtual, 20) : new List<ImovelLead>();

        Imovel = MapearFormulario(idSalao, idAtual);
        Imovel.Fotos = fotosExistentes;
        Imovel.Leads = leadsExistentes;
        ButtonText = Imovel.IdImovel > 0 ? "Atualizar" : "Cadastrar";

        if (Imovel.IdImovel > 0 && _imovelHandler.ObterPorIdESalao(Imovel.IdImovel, idSalao) == null)
        {
            Mensagem = "Imovel nao encontrado para a empresa atual.";
            MensagemTipo = "danger";
            return Page();
        }

        var fotosRemovidas = ObterIdsSelecionados("removerFotoIds");
        var totalFotosRestantes = fotosExistentes.Count(f => !fotosRemovidas.Contains(f.IdFoto)) + Request.Form.Files.GetFiles("fotos").Count;

        if (!ValidarImovel(totalFotosRestantes))
        {
            return Page();
        }

        try
        {
            Imovel.IdImovel = _imovelHandler.Salvar(Imovel);
            await ProcessarFotosAsync(Imovel.IdImovel, fotosExistentes, fotosRemovidas);
            AtualizarImagemCompartilhamentoPelaCapa();

            Imovel = _imovelHandler.ObterPorIdESalao(Imovel.IdImovel, idSalao) ?? Imovel;
            ButtonText = "Atualizar";
            TempData["ImoveisMensagem"] = "Imovel salvo com sucesso.";
            TempData["ImoveisMensagemTipo"] = "success";
            return RedirectToPage("/Imoveis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar imovel {CodigoImovel} para o salao {IdSalao}", Imovel.CodigoImovel, idSalao);
            Mensagem = "Nao foi possivel salvar o imovel. Tente novamente ou entre em contato com o suporte.";
            MensagemDetalheTecnico = ObterDetalheTecnico(ex);
            MensagemTipo = "danger";
        }

        return Page();
    }

    private Imovel MapearFormulario(int idSalao, int idImovel)
    {
        var dataCadastro = ParseDate(Request.Form["dataCadastro"]) ?? DateTime.Today;
        var dataAtualizacao = ParseDate(Request.Form["dataAtualizacao"]) ?? DateTime.Today;

        var imovel = new Imovel
        {
            IdImovel = idImovel,
            IdSalao = idSalao,
            CodigoImovel = Campo("codigoImovel"),
            StatusAnuncio = Campo("statusAnuncio", "Rascunho"),
            Finalidade = Campo("finalidade", "Venda"),
            TipoImovel = Campo("tipoImovel", "Casa"),
            Titulo = Campo("titulo"),
            Subtitulo = CampoOpcional("subtitulo"),
            ImobiliariaResponsavel = Campo("imobiliariaResponsavel"),
            CreciResponsavelLegal = Campo("creciResponsavelLegal"),
            DataCadastro = dataCadastro,
            DataAtualizacao = dataAtualizacao,
            ValorVenda = ParseNullableDecimal(Request.Form["valorVenda"]),
            ValorAluguel = ParseNullableDecimal(Request.Form["valorAluguel"]),
            ValorCondominio = ParseNullableDecimal(Request.Form["valorCondominio"]),
            ValorIPTU = ParseNullableDecimal(Request.Form["valorIPTU"]),
            PrecoSobConsulta = FormBool("precoSobConsulta"),
            AceitaFinanciamento = FormBool("aceitaFinanciamento"),
            AceitaPermuta = FormBool("aceitaPermuta"),
            ObservacoesComerciais = CampoOpcional("observacoesComerciais"),
            AvisoAlteracaoPreco = Campo("avisoAlteracaoPreco", "Precos e disponibilidade podem mudar sem aviso previo."),
            Estado = Campo("estado"),
            Cidade = Campo("cidade"),
            Bairro = Campo("bairro"),
            Logradouro = Campo("logradouro"),
            Numero = CampoOpcional("numero"),
            Complemento = CampoOpcional("complemento"),
            CEP = CampoOpcional("cep"),
            Latitude = ParseNullableDecimal(Request.Form["latitude"]),
            Longitude = ParseNullableDecimal(Request.Form["longitude"]),
            ExibirEnderecoCompleto = FormBool("exibirEnderecoCompleto"),
            TextoReferenciaRegiao = CampoOpcional("textoReferenciaRegiao"),
            AreaConstruidaPrivativa = ParseNullableDecimal(Request.Form["areaConstruidaPrivativa"]),
            AreaAproximada = FormBool("areaAproximada"),
            AreaLoteTerreno = ParseNullableDecimal(Request.Form["areaLoteTerreno"]),
            AreaLoteAproximada = FormBool("areaLoteAproximada"),
            Quartos = ParseNullableInt(Request.Form["quartos"]),
            Suites = ParseNullableInt(Request.Form["suites"]),
            Banheiros = ParseNullableInt(Request.Form["banheiros"]),
            Lavabos = ParseNullableInt(Request.Form["lavabos"]),
            VagasGaragem = ParseNullableInt(Request.Form["vagasGaragem"]),
            Salas = ParseNullableInt(Request.Form["salas"]),
            Varandas = ParseNullableInt(Request.Form["varandas"]),
            Closets = ParseNullableInt(Request.Form["closets"]),
            Depositos = ParseNullableInt(Request.Form["depositos"]),
            Piscina = FormBool("piscina"),
            ArCondicionado = FormBool("arCondicionado"),
            Churrasqueira = FormBool("churrasqueira"),
            Sauna = FormBool("sauna"),
            Jardim = FormBool("jardim"),
            DependenciaEmpregadaDCE = FormBool("dependenciaEmpregadaDCE"),
            AreaGourmet = FormBool("areaGourmet"),
            Jacuzzi = FormBool("jacuzzi"),
            Hidromassagem = FormBool("hidromassagem"),
            Escritorio = FormBool("escritorio"),
            SalaTV = FormBool("salaTV"),
            CozinhaPlanejada = FormBool("cozinhaPlanejada"),
            ClosetCaracteristica = FormBool("closetCaracteristica"),
            VarandaCaracteristica = FormBool("varandaCaracteristica"),
            LavaboCaracteristica = FormBool("lavaboCaracteristica"),
            DescricaoPrincipal = Campo("descricaoPrincipal"),
            ListaComposicao = CampoOpcional("listaComposicao"),
            DestaquesImovel = CampoOpcional("destaquesImovel"),
            ObservacoesFinais = CampoOpcional("observacoesFinais"),
            TextoDisclaimer = Campo("textoDisclaimer", "Informacoes sujeitas a confirmacao junto a imobiliaria responsavel."),
            VideoUrl = CampoOpcional("videoUrl"),
            TourVirtualUrl = CampoOpcional("tourVirtualUrl"),
            PlantaArquivoUrl = CampoOpcional("plantaArquivoUrl"),
            NomeImobiliariaContato = Campo("nomeImobiliariaContato"),
            TelefonePrincipal = Campo("telefonePrincipal"),
            WhatsApp = Campo("whatsApp"),
            EmailContato = Campo("emailContato"),
            TextoBotaoWhatsApp = Campo("textoBotaoWhatsApp", "Enviar WhatsApp"),
            MensagemPadraoWhatsApp = Campo("mensagemPadraoWhatsApp", "Ola, tenho interesse no imovel codigo {codigo}."),
            PermitirVerTelefone = FormBool("permitirVerTelefone"),
            ReceberNovidades = FormBool("receberNovidades"),
            TermosPrivacidadeTexto = Campo("termosPrivacidadeTexto", "Ao enviar os dados, o interessado aceita os termos de privacidade e contato."),
            SlugUrl = CampoOpcional("slugUrl") ?? string.Empty,
            TituloSEO = Campo("tituloSEO"),
            MetaDescription = Campo("metaDescription"),
            ImagemCompartilhamento = CampoOpcional("imagemCompartilhamento") ?? string.Empty,
            TextoCompartilhamento = CampoOpcional("textoCompartilhamento"),
            PermitirCompartilhamento = FormBool("permitirCompartilhamento"),
            PublicarNoSite = FormBool("publicarNoSite"),
            DestaqueNaBusca = FormBool("destaqueNaBusca"),
            TagsInternas = CampoOpcional("tagsInternas"),
            IndexarGoogle = FormBool("indexarGoogle"),
            ImovelDisponivel = FormBool("imovelDisponivel"),
            OrdemPrioridade = ParseNullableInt(Request.Form["ordemPrioridade"]),
            OrigemCadastro = CampoOpcional("origemCadastro") ?? "Manual",
            IdExterno = CampoOpcional("idExterno"),
            Excluido = false
        };

        if (string.IsNullOrWhiteSpace(imovel.SlugUrl))
        {
            imovel.SlugUrl = GerarSlug($"{imovel.Finalidade} {imovel.TipoImovel} {imovel.Estado} {imovel.Cidade} {imovel.Bairro} {imovel.CodigoImovel}");
        }

        if (string.IsNullOrWhiteSpace(imovel.TituloSEO))
        {
            imovel.TituloSEO = imovel.Titulo;
        }

        if (string.IsNullOrWhiteSpace(imovel.MetaDescription))
        {
            imovel.MetaDescription = $"{imovel.TipoImovel} para {imovel.Finalidade.ToLowerInvariant()} em {imovel.Bairro}, {imovel.Cidade}.";
        }

        if (string.IsNullOrWhiteSpace(imovel.NomeImobiliariaContato))
        {
            imovel.NomeImobiliariaContato = imovel.ImobiliariaResponsavel;
        }

        return imovel;
    }

    private bool ValidarImovel(int totalFotosRestantes)
    {
        if (string.IsNullOrWhiteSpace(Imovel.CodigoImovel))
        {
            return Falha("Informe o codigo do imovel.");
        }

        if (_imovelHandler.ExisteCodigoPorSalao(Imovel.IdSalao, Imovel.CodigoImovel, Imovel.IdImovel > 0 ? Imovel.IdImovel : null))
        {
            return Falha("Ja existe um imovel com este codigo para a empresa atual.");
        }

        if (string.IsNullOrWhiteSpace(Imovel.Titulo) ||
            string.IsNullOrWhiteSpace(Imovel.ImobiliariaResponsavel) ||
            string.IsNullOrWhiteSpace(Imovel.CreciResponsavelLegal))
        {
            return Falha("Preencha os dados obrigatorios do anuncio.");
        }

        if (!Imovel.PrecoSobConsulta)
        {
            if (string.Equals(Imovel.Finalidade, "Aluguel", StringComparison.OrdinalIgnoreCase) && (!Imovel.ValorAluguel.HasValue || Imovel.ValorAluguel <= 0))
            {
                return Falha("Informe o valor de aluguel ou marque preco sob consulta.");
            }

            if (!string.Equals(Imovel.Finalidade, "Aluguel", StringComparison.OrdinalIgnoreCase) && (!Imovel.ValorVenda.HasValue || Imovel.ValorVenda <= 0))
            {
                return Falha("Informe o valor de venda ou marque preco sob consulta.");
            }
        }

        if (string.IsNullOrWhiteSpace(Imovel.Estado) ||
            string.IsNullOrWhiteSpace(Imovel.Cidade) ||
            string.IsNullOrWhiteSpace(Imovel.Bairro) ||
            string.IsNullOrWhiteSpace(Imovel.Logradouro))
        {
            return Falha("Preencha os campos obrigatorios de localizacao.");
        }

        if (!Imovel.AreaConstruidaPrivativa.HasValue || Imovel.AreaConstruidaPrivativa <= 0 ||
            !Imovel.Quartos.HasValue ||
            !Imovel.Banheiros.HasValue ||
            !Imovel.VagasGaragem.HasValue)
        {
            return Falha("Preencha area, quartos, banheiros e vagas.");
        }

        if (string.IsNullOrWhiteSpace(Imovel.DescricaoPrincipal))
        {
            return Falha("Informe a descricao principal do imovel.");
        }

        if (string.IsNullOrWhiteSpace(Imovel.TelefonePrincipal) ||
            string.IsNullOrWhiteSpace(Imovel.WhatsApp) ||
            string.IsNullOrWhiteSpace(Imovel.EmailContato))
        {
            return Falha("Preencha telefone, WhatsApp e e-mail de contato.");
        }

        if (string.IsNullOrWhiteSpace(Imovel.SlugUrl) ||
            string.IsNullOrWhiteSpace(Imovel.TituloSEO) ||
            string.IsNullOrWhiteSpace(Imovel.MetaDescription))
        {
            return Falha("Preencha os campos obrigatorios de SEO.");
        }

        return true;
    }

    private async Task ProcessarFotosAsync(int idImovel, List<ImovelFoto> fotosExistentes, HashSet<int> fotosRemovidas)
    {
        var fotoCapaId = ParseNullableInt(Request.Form["fotoCapaId"]);

        foreach (var foto in fotosExistentes)
        {
            if (fotosRemovidas.Contains(foto.IdFoto))
            {
                RemoverArquivoFisico(foto.CaminhoArquivo);
                _imovelHandler.RemoverFoto(foto.IdFoto, idImovel);
                continue;
            }

            foto.Ordem = ParseNullableInt(Request.Form[$"fotoOrdem_{foto.IdFoto}"]) ?? foto.Ordem;
            foto.Legenda = CampoOpcional($"fotoLegenda_{foto.IdFoto}");
            foto.AltText = CampoOpcional($"fotoAlt_{foto.IdFoto}");
            foto.FotoCapa = fotoCapaId == foto.IdFoto;
            _imovelHandler.AtualizarFoto(foto);
        }

        var fotos = Request.Form.Files.GetFiles("fotos");
        if (!fotos.Any())
        {
            return;
        }

        var pasta = ObterPastaUpload();
        Directory.CreateDirectory(pasta);
        var proximaOrdem = fotosExistentes.Any() ? fotosExistentes.Max(f => f.Ordem) + 1 : 1;
        var deveMarcarPrimeiraComoCapa = !fotoCapaId.HasValue && !fotosExistentes.Any(f => !fotosRemovidas.Contains(f.IdFoto) && f.FotoCapa);
        var codigoArquivo = SanitizarParteNomeArquivo(Imovel.CodigoImovel);
        if (string.IsNullOrWhiteSpace(codigoArquivo))
        {
            codigoArquivo = idImovel.ToString(CultureInfo.InvariantCulture);
        }

        foreach (var arquivo in fotos)
        {
            if (arquivo.Length <= 0)
            {
                continue;
            }

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(extensao))
            {
                continue;
            }

            var nomeOriginal = SanitizarParteNomeArquivo(Path.GetFileNameWithoutExtension(arquivo.FileName));
            if (string.IsNullOrWhiteSpace(nomeOriginal))
            {
                nomeOriginal = "foto";
            }

            var nomeArquivo = ObterNomeArquivoDisponivel(pasta, $"{codigoArquivo}_{nomeOriginal}", extensao);
            var caminhoFisico = Path.Combine(pasta, nomeArquivo);
            await using (var stream = System.IO.File.Create(caminhoFisico))
            {
                await arquivo.CopyToAsync(stream);
            }

            _imovelHandler.AdicionarFoto(new ImovelFoto
            {
                IdImovel = idImovel,
                CaminhoArquivo = $"/uploads/imoveis/{nomeArquivo}",
                Ordem = proximaOrdem++,
                FotoCapa = deveMarcarPrimeiraComoCapa,
                Legenda = null,
                AltText = Imovel.Titulo
            });

            deveMarcarPrimeiraComoCapa = false;
        }
    }

    private void AtualizarImagemCompartilhamentoPelaCapa()
    {
        if (!string.IsNullOrWhiteSpace(Imovel.ImagemCompartilhamento))
        {
            return;
        }

        var capa = _imovelHandler.ListarFotos(Imovel.IdImovel).FirstOrDefault(f => f.FotoCapa)
            ?? _imovelHandler.ListarFotos(Imovel.IdImovel).FirstOrDefault();

        if (capa == null)
        {
            return;
        }

        Imovel.ImagemCompartilhamento = capa.CaminhoArquivo;
        _imovelHandler.Salvar(Imovel);
    }

    private string ObterPastaUpload()
    {
        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        return Path.Combine(webRoot, "uploads", "imoveis");
    }

    private static string ObterNomeArquivoDisponivel(string pasta, string nomeBase, string extensao)
    {
        nomeBase = nomeBase.Length > 120 ? nomeBase[..120].Trim('-', '_', '.') : nomeBase;
        var nomeArquivo = $"{nomeBase}{extensao}";
        var contador = 2;

        while (System.IO.File.Exists(Path.Combine(pasta, nomeArquivo)))
        {
            nomeArquivo = $"{nomeBase}_{contador}{extensao}";
            contador++;
        }

        return nomeArquivo;
    }

    private static string SanitizarParteNomeArquivo(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var normalizado = valor.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else if (c == '-' || c == '_')
            {
                builder.Append(c);
            }
            else if (char.IsWhiteSpace(c) || c == '.')
            {
                builder.Append('-');
            }
        }

        var limpo = Regex.Replace(builder.ToString(), @"[-_]{2,}", "-").Trim('-', '_', '.');
        return limpo;
    }

    private void RemoverArquivoFisico(string caminhoArquivo)
    {
        if (string.IsNullOrWhiteSpace(caminhoArquivo))
        {
            return;
        }

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var relativo = caminhoArquivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var caminho = Path.GetFullPath(Path.Combine(webRoot, relativo));
        var raiz = Path.GetFullPath(webRoot);
        if (caminho.StartsWith(raiz, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(caminho))
        {
            System.IO.File.Delete(caminho);
        }
    }

    private bool Falha(string mensagem)
    {
        Mensagem = mensagem;
        MensagemTipo = "warning";
        return false;
    }

    private static string ObterDetalheTecnico(Exception ex)
    {
        var mensagens = new List<string>();

        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            mensagens.Add(ex.Message);
        }

        var detalheRaiz = ex.GetBaseException()?.Message;
        if (!string.IsNullOrWhiteSpace(detalheRaiz) &&
            !string.Equals(detalheRaiz, ex.Message, StringComparison.Ordinal))
        {
            mensagens.Add(detalheRaiz);
        }

        var detalhe = string.Join(" | ", mensagens.Distinct());
        detalhe = Regex.Replace(detalhe, @"\s+", " ").Trim();

        const int limite = 600;
        return detalhe.Length > limite ? $"{detalhe[..limite]}..." : detalhe;
    }

    private static Imovel CriarNovoImovel() => new()
    {
        StatusAnuncio = "Rascunho",
        Finalidade = "Venda",
        TipoImovel = "Casa",
        DataCadastro = DateTime.Today,
        DataAtualizacao = DateTime.Today,
        AvisoAlteracaoPreco = "Precos e disponibilidade podem mudar sem aviso previo.",
        TextoDisclaimer = "Informacoes sujeitas a confirmacao junto a imobiliaria responsavel.",
        TextoBotaoWhatsApp = "Enviar WhatsApp",
        MensagemPadraoWhatsApp = "Ola, tenho interesse no imovel codigo {codigo}.",
        PermitirVerTelefone = true,
        TermosPrivacidadeTexto = "Ao enviar os dados, o interessado aceita os termos de privacidade e contato.",
        PermitirCompartilhamento = true,
        IndexarGoogle = true,
        ImovelDisponivel = true,
        OrigemCadastro = "Manual",
        ExibirEnderecoCompleto = true
    };

    private string Campo(string nome, string valorPadrao = "")
    {
        var valor = Request.Form[nome].ToString().Trim();
        return string.IsNullOrWhiteSpace(valor) ? valorPadrao : valor;
    }

    private string? CampoOpcional(string nome)
    {
        var valor = Request.Form[nome].ToString().Trim();
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private bool FormBool(string nome)
    {
        var valor = Request.Form[nome].ToString();
        return valor == "on" || valor == "true" || valor == "True" || valor == "1";
    }

    private static int? ParseNullableInt(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return int.TryParse(valor.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero) ? numero : null;
    }

    private static decimal? ParseNullableDecimal(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        if (decimal.TryParse(normalizado, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, new CultureInfo("pt-BR"), out var ptBr))
        {
            return ptBr;
        }

        return decimal.TryParse(normalizado, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : null;
    }

    private static DateTime? ParseDate(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : null;
    }

    private HashSet<int> ObterIdsSelecionados(string nome)
    {
        return Request.Form[nome]
            .Select(valor => ParseNullableInt(valor))
            .Where(valor => valor.HasValue)
            .Select(valor => valor!.Value)
            .ToHashSet();
    }

    private static string GerarSlug(string texto)
    {
        texto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in texto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        var semAcento = builder.ToString().Normalize(NormalizationForm.FormC);
        var slug = Regex.Replace(semAcento, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
    }

    private bool TryObterIdSalao(out int idSalao)
    {
        return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
    }
}
