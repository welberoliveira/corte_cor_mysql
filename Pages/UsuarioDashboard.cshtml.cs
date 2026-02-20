using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MercadoPago.Config;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Common;
using MercadoPago.Resource.Payment;
using MercadoPago.Client;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class UsuarioDashboardModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string ErrorMessage { get; set; }

        public int IdUsuario { get; set; }
        public string NomeUsuario { get; set; }
        public string NomeSalao { get; set; }
        public List<Salao> Saloes { get; set; }
        public DatabaseHandler dbHandler = new();

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

        public UsuarioDashboardModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            string emailUsuario = User.Identity.Name;

            var handler = new UsuarioHandler();
            var Usuario = handler.Listar().Where(m => m.Email == emailUsuario).ToList();
            int IdUsuario = Usuario.FirstOrDefault().IdUsuario;

            // Buscar nome do cliente
            string queryNomeUsuario = "SELECT Nome FROM CorteCor_Usuario WHERE IdUsuario = @IdUsuario";
            using (var connection = dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = queryNomeUsuario;
                command.AddWithValue("@IdUsuario", IdUsuario);
                NomeUsuario = command.ExecuteScalar()?.ToString() ?? "_";
            }
            
            var SalaoHandler = new SalaoHandler();
            Saloes = SalaoHandler.Listar();

            NomeSalao = Saloes.FirstOrDefault(p => p.IdSalao == Usuario.FirstOrDefault().IdSalao).Nome ?? "Erro";
        }
    }
}

