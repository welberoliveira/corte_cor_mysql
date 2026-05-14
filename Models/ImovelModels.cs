namespace CorteCor.Models;

public class Imovel
{
    public int IdImovel { get; set; }
    public int IdSalao { get; set; }
    public string CodigoImovel { get; set; } = string.Empty;
    public string StatusAnuncio { get; set; } = "Rascunho";
    public string Finalidade { get; set; } = "Venda";
    public string TipoImovel { get; set; } = "Casa";
    public string Titulo { get; set; } = string.Empty;
    public string? Subtitulo { get; set; }
    public string ImobiliariaResponsavel { get; set; } = string.Empty;
    public string CreciResponsavelLegal { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.Today;
    public DateTime DataAtualizacao { get; set; } = DateTime.Today;

    public decimal? ValorVenda { get; set; }
    public decimal? ValorAluguel { get; set; }
    public decimal? ValorCondominio { get; set; }
    public decimal? ValorIPTU { get; set; }
    public bool PrecoSobConsulta { get; set; }
    public bool AceitaFinanciamento { get; set; }
    public bool AceitaPermuta { get; set; }
    public string? ObservacoesComerciais { get; set; }
    public string AvisoAlteracaoPreco { get; set; } = "Precos e disponibilidade podem mudar sem aviso previo.";

    public string Estado { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? CEP { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool ExibirEnderecoCompleto { get; set; } = true;
    public string? TextoReferenciaRegiao { get; set; }

    public decimal? AreaConstruidaPrivativa { get; set; }
    public bool AreaAproximada { get; set; }
    public decimal? AreaLoteTerreno { get; set; }
    public bool AreaLoteAproximada { get; set; }
    public int? Quartos { get; set; }
    public int? Suites { get; set; }
    public int? Banheiros { get; set; }
    public int? Lavabos { get; set; }
    public int? VagasGaragem { get; set; }
    public int? Salas { get; set; }
    public int? Varandas { get; set; }
    public int? Closets { get; set; }
    public int? Depositos { get; set; }

    public bool Piscina { get; set; }
    public bool ArCondicionado { get; set; }
    public bool Churrasqueira { get; set; }
    public bool Sauna { get; set; }
    public bool Jardim { get; set; }
    public bool DependenciaEmpregadaDCE { get; set; }
    public bool AreaGourmet { get; set; }
    public bool Jacuzzi { get; set; }
    public bool Hidromassagem { get; set; }
    public bool Escritorio { get; set; }
    public bool SalaTV { get; set; }
    public bool CozinhaPlanejada { get; set; }
    public bool ClosetCaracteristica { get; set; }
    public bool VarandaCaracteristica { get; set; }
    public bool LavaboCaracteristica { get; set; }

    public string DescricaoPrincipal { get; set; } = string.Empty;
    public string? ListaComposicao { get; set; }
    public string? DestaquesImovel { get; set; }
    public string? ObservacoesFinais { get; set; }
    public string TextoDisclaimer { get; set; } = "Informacoes sujeitas a confirmacao junto a imobiliaria responsavel.";

    public string? VideoUrl { get; set; }
    public string? TourVirtualUrl { get; set; }
    public string? PlantaArquivoUrl { get; set; }

    public string NomeImobiliariaContato { get; set; } = string.Empty;
    public string TelefonePrincipal { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string EmailContato { get; set; } = string.Empty;
    public string TextoBotaoWhatsApp { get; set; } = "Enviar WhatsApp";
    public string MensagemPadraoWhatsApp { get; set; } = "Ola, tenho interesse no imovel codigo {codigo}.";
    public bool PermitirVerTelefone { get; set; } = true;
    public bool ReceberNovidades { get; set; }
    public string TermosPrivacidadeTexto { get; set; } = "Ao enviar os dados, o interessado aceita os termos de privacidade e contato.";

    public string SlugUrl { get; set; } = string.Empty;
    public string TituloSEO { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string ImagemCompartilhamento { get; set; } = string.Empty;
    public string? TextoCompartilhamento { get; set; }
    public bool PermitirCompartilhamento { get; set; } = true;

    public bool PublicarNoSite { get; set; }
    public bool DestaqueNaBusca { get; set; }
    public string? TagsInternas { get; set; }
    public bool IndexarGoogle { get; set; } = true;
    public bool ImovelDisponivel { get; set; } = true;
    public int? OrdemPrioridade { get; set; }
    public string? OrigemCadastro { get; set; } = "Manual";
    public string? IdExterno { get; set; }
    public bool Excluido { get; set; }

    public int QuantidadeFotos { get; set; }
    public int QuantidadeLeads { get; set; }
    public string? FotoCapaUrl { get; set; }
    public List<ImovelFoto> Fotos { get; set; } = new();
    public List<ImovelLead> Leads { get; set; } = new();
}

public class ImovelWebFiltro
{
    public string? Pesquisa { get; set; }
    public string? StatusAnuncio { get; set; }
    public string? Finalidade { get; set; }
    public string? TipoImovel { get; set; }
    public string? Estado { get; set; }
    public string? Cidade { get; set; }
    public string? Bairro { get; set; }
    public decimal? ValorMinimo { get; set; }
    public decimal? ValorMaximo { get; set; }
    public decimal? AreaMinima { get; set; }
    public decimal? AreaMaxima { get; set; }
    public int? QuartosMinimo { get; set; }
    public int? SuitesMinimo { get; set; }
    public int? BanheirosMinimo { get; set; }
    public int? VagasMinimo { get; set; }
    public bool SomentePublicados { get; set; } = true;
    public bool SomenteDisponiveis { get; set; } = true;
    public bool DestaqueNaBusca { get; set; }
    public bool PrecoSobConsulta { get; set; }
    public bool AceitaFinanciamento { get; set; }
    public bool AceitaPermuta { get; set; }
    public bool ComVideo { get; set; }
    public bool ComTourVirtual { get; set; }
    public bool ComFotos { get; set; }
    public bool Piscina { get; set; }
    public bool ArCondicionado { get; set; }
    public bool Churrasqueira { get; set; }
    public bool Sauna { get; set; }
    public bool Jardim { get; set; }
    public bool AreaGourmet { get; set; }
    public bool Jacuzzi { get; set; }
    public bool Hidromassagem { get; set; }
    public bool Escritorio { get; set; }
    public bool SalaTV { get; set; }
    public bool CozinhaPlanejada { get; set; }
    public bool Closet { get; set; }
    public bool Varanda { get; set; }
    public bool Lavabo { get; set; }
    public string? Ordenacao { get; set; }
}

public class ImovelFoto
{
    public int IdFoto { get; set; }
    public int IdImovel { get; set; }
    public string CaminhoArquivo { get; set; } = string.Empty;
    public bool FotoCapa { get; set; }
    public int Ordem { get; set; }
    public string? Legenda { get; set; }
    public string? AltText { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;
}

public class ImovelLead
{
    public int IdLead { get; set; }
    public int IdImovel { get; set; }
    public string NomeInteressado { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TelefoneWhatsapp { get; set; } = string.Empty;
    public string? Mensagem { get; set; }
    public bool AceiteTermos { get; set; }
    public bool AceitaReceberNovidades { get; set; }
    public string Status { get; set; } = "Novo";
    public string? Origem { get; set; }
    public string? IpOrigem { get; set; }
    public string? UserAgent { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;
}
