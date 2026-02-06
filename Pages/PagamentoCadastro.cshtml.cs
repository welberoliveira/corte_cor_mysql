using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PagamentoCadastroModel : PageModel
    {
        public Pagamento Pagamento { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            if (id.HasValue)
            {
                var handler = new PagamentoHandler();
                Pagamento = handler.Listar().FirstOrDefault(p => p.IdPagamento == id.Value);
                ButtonText = "Atualizar";
            }
        }

        private static decimal ParseDecimalBR(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;
            valor = valor.Trim().Replace(".", "").Replace(",", ".");
            return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static DateTime ParseDateTimeLocal(string valor)
        {
            // Espera "yyyy-MM-ddTHH:mm"
            if (string.IsNullOrWhiteSpace(valor)) return DateTime.Now;
            return DateTime.Parse(valor);
        }

        public void OnPost()
        {
            int id = 0;
            int.TryParse(Request.Form["id"], out id);

            int idAgendamento = 0;
            int.TryParse(Request.Form["idAgendamento"], out idAgendamento);

            int idMeioPagamento = 0;
            int.TryParse(Request.Form["idMeioPagamento"], out idMeioPagamento);

            decimal valor = ParseDecimalBR(Request.Form["valor"]);
            DateTime data = ParseDateTimeLocal(Request.Form["data"]);

            var pagamento = new Pagamento
            {
                IdPagamento = id,
                IdAgendamento = idAgendamento,
                IdMeioPagamento = idMeioPagamento,

                Tipo = Request.Form["tipo"],
                Valor = valor,
                Data = data,

                Contos = Request.Form["contos"],
                Campos = Request.Form["campos"]
            };

            var handler = new PagamentoHandler();

            if (id > 0)
            {
                handler.Atualizar(pagamento);
                Mensagem = "Pagamento atualizado com sucesso!";
            }
            else
            {
                id = handler.CadastrarPagamento(pagamento);
                Mensagem = "Pagamento cadastrado com sucesso!";
            }

            OnGet(id > 0 ? id : (int?)null);
        }
    }
}
