using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CorteCor.Models.Ginfes
{
    [XmlRoot(ElementName = "EnviarLoteRpsEnvio", Namespace = "http://www.ginfes.com.br/servico_enviar_lote_rps_envio_v03.xsd")]
    public class EnviarLoteRpsEnvio
    {
        [XmlElement(ElementName = "LoteRps")]
        public LoteRps LoteRps { get; set; } = new LoteRps();
    }

    public class LoteRps
    {
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "versao")]
        public string Versao { get; set; } = "3";

        [XmlElement(ElementName = "NumeroLote")]
        public int NumeroLote { get; set; }

        [XmlElement(ElementName = "Cnpj", Namespace = "http://www.ginfes.com.br/tipos_v03.xsd")]
        public string Cnpj { get; set; }

        [XmlElement(ElementName = "InscricaoMunicipal", Namespace = "http://www.ginfes.com.br/tipos_v03.xsd")]
        public string InscricaoMunicipal { get; set; }

        [XmlElement(ElementName = "QuantidadeRps")]
        public int QuantidadeRps { get; set; }

        [XmlElement(ElementName = "ListaRps")]
        public ListaRps ListaRps { get; set; } = new ListaRps();
    }

    public class ListaRps
    {
        [XmlElement(ElementName = "Rps")]
        public List<Rps> Rps { get; set; } = new List<Rps>();
    }

    public class Rps
    {
        [XmlElement(ElementName = "InfRps", Namespace = "http://www.ginfes.com.br/tipos_v03.xsd")]
        public InfRps InfRps { get; set; } = new InfRps();
    }

    public class InfRps
    {
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }

        [XmlElement(ElementName = "IdentificacaoRps")]
        public IdentificacaoRps IdentificacaoRps { get; set; } = new IdentificacaoRps();

        [XmlElement(ElementName = "DataEmissao")]
        public string DataEmissao { get; set; } // yyyy-MM-ddTHH:mm:ss

        [XmlElement(ElementName = "NaturezaOperacao")]
        public int NaturezaOperacao { get; set; }

        [XmlElement(ElementName = "OptanteSimplesNacional")]
        public int OptanteSimplesNacional { get; set; }

        [XmlElement(ElementName = "IncentivadorCultural")]
        public int IncentivadorCultural { get; set; }

        [XmlElement(ElementName = "Status")]
        public int Status { get; set; }

        [XmlElement(ElementName = "Servico")]
        public ServicoRps Servico { get; set; } = new ServicoRps();

        [XmlElement(ElementName = "Prestador")]
        public PrestadorRps Prestador { get; set; } = new PrestadorRps();

        [XmlElement(ElementName = "Tomador")]
        public TomadorRps Tomador { get; set; } = new TomadorRps();
    }

    public class IdentificacaoRps
    {
        [XmlElement(ElementName = "Numero")]
        public int Numero { get; set; }

        [XmlElement(ElementName = "Serie")]
        public string Serie { get; set; }

        [XmlElement(ElementName = "Tipo")]
        public string Tipo { get; set; }
    }

    public class ServicoRps
    {
        [XmlElement(ElementName = "Valores")]
        public ValoresRps Valores { get; set; } = new ValoresRps();

        [XmlElement(ElementName = "ItemListaServico")]
        public string ItemListaServico { get; set; }

        [XmlElement(ElementName = "CodigoTributacaoMunicipio")]
        public string CodigoTributacaoMunicipio { get; set; }

        [XmlElement(ElementName = "Discriminacao")]
        public string Discriminacao { get; set; }

        [XmlElement(ElementName = "CodigoMunicipio")]
        public string CodigoMunicipio { get; set; }
    }

    public class ValoresRps
    {
        [XmlElement(ElementName = "ValorServicos")]
        public decimal ValorServicos { get; set; }

        [XmlElement(ElementName = "IssRetido")]
        public int IssRetido { get; set; }

        [XmlElement(ElementName = "ValorIss", IsNullable = false)]
        public decimal? ValorIss { get; set; }

        public bool ValorIssSpecified { get { return ValorIss.HasValue; } }

        [XmlElement(ElementName = "BaseCalculo")]
        public decimal BaseCalculo { get; set; }

        [XmlElement(ElementName = "Aliquota")]
        public decimal Aliquota { get; set; }
    }

    public class PrestadorRps
    {
        [XmlElement(ElementName = "Cnpj")]
        public string Cnpj { get; set; }

        [XmlElement(ElementName = "InscricaoMunicipal")]
        public string InscricaoMunicipal { get; set; }
    }

    public class TomadorRps
    {
        [XmlElement(ElementName = "IdentificacaoTomador")]
        public IdentificacaoTomador IdentificacaoTomador { get; set; } = new IdentificacaoTomador();

        [XmlElement(ElementName = "RazaoSocial")]
        public string RazaoSocial { get; set; }
    }

    public class IdentificacaoTomador
    {
        [XmlElement(ElementName = "CpfCnpj")]
        public CpfCnpj CpfCnpj { get; set; } = new CpfCnpj();
    }

    public class CpfCnpj
    {
        [XmlElement(ElementName = "Cpf")]
        public string Cpf { get; set; }

        [XmlElement(ElementName = "Cnpj")]
        public string Cnpj { get; set; }
    }
}
