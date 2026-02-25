using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ServicoCadastroModel : PageModel
    {
        public Servico Servico { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            if (id.HasValue)
            {
                var handler = new ServicoHandler();
                Servico = handler.Listar().FirstOrDefault(p => p.IdServico == id.Value);
                ButtonText = "Atualizar";
            }
        }

        private static decimal ParsePrecoBR(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;

            // Ex: "1.234,56" -> "1234.56"
            valor = valor.Trim().Replace(".", "").Replace(",", ".");
            return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static TimeSpan ParseDuracao(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return TimeSpan.Zero;

            // Esperado do input type="time": "HH:mm"
            if (TimeSpan.TryParseExact(valor.Trim(), @"hh\:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                out var ts))
                return ts;

            // fallback
            return TimeSpan.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        }


        public void OnPost()
        {
            int id = 0;
            int.TryParse(Request.Form["id"], out id);

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            decimal preco = ParsePrecoBR(Request.Form["preco"]);

            TimeSpan duracao = ParseDuracao(Request.Form["duracao"]);

            decimal? aliquotaIss = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["aliquotaISS"]))
            {
                aliquotaIss = ParsePrecoBR(Request.Form["aliquotaISS"]);
            }

            var servico = new Servico
            {
                IdServico = id,
                Nome = Request.Form["nome"],
                Preco = preco,

                Duracao = duracao,
                IdSalao = idSalao,

                CodigoTributacaoMunicipio = Request.Form["codigoTributacao"],
                Cnae = Request.Form["cnae"].ToString()?.Replace(".", "").Replace("-", "").Replace("/", ""),
                AliquotaISS = aliquotaIss
            };

            var handler = new ServicoHandler();

            if (id > 0)
            {
                handler.Atualizar(servico);
                Mensagem = "Servi?o atualizado com sucesso!";
            }
            else
            {
                id = handler.CadastrarServico(servico);
                Mensagem = "Servi?o cadastrado com sucesso!";
            }

            OnGet(id > 0 ? id : (int?)null);
        }
    }
}

