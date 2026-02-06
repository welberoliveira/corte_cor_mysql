using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Data.SqlClient;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class PainelModel : PageModel
    {
        public Dictionary<string, int> DocumentacaoChartData { get; set; }
        public Dictionary<string, int> TipoImovelChartData { get; set; }
        public Dictionary<string, int> TipoEdificacaoChartData { get; set; }
        public Dictionary<string, int> AcabamentoChartData { get; set; }
        public Dictionary<string, int> AguaPotavelChartData { get; set; }
        public Dictionary<string, int> EsgotamentoSanitarioChartData { get; set; }
        public Dictionary<string, int> EnergiaEletricaChartData { get; set; }
        public Dictionary<string, int> DestinoLixoChartData { get; set; }
        public Dictionary<string, int> CondicaoOcupacaoLoteChartData { get; set; }
        public Dictionary<string, int> NumeroOcupacaoChartData { get; set; }
        public Dictionary<string, int> PossuiOutroImovelChartData { get; set; }
        public Dictionary<string, int> PossuiIPTUChartData { get; set; }

        public void OnGet()
        {

        }
    }

}
