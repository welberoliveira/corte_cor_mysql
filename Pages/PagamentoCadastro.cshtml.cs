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

        public void OnGet(Guid? id)
        {
            if (id.HasValue && id.Value != Guid.Empty)
            {
                var handler = new PagamentoHandler();
                Pagamento = handler.ObterPorId(id.Value);
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
            Guid id = Guid.Empty;
            Guid.TryParse(Request.Form["id"], out id);

            int idAgendamento = 0;
            int.TryParse(Request.Form["idAgendamento"], out idAgendamento);

            int idMeioPagamento = 0;
            int.TryParse(Request.Form["idMeioPagamento"], out idMeioPagamento);

            decimal valor = ParseDecimalBR(Request.Form["valor"]);
            DateTime data = ParseDateTimeLocal(Request.Form["data"]);

            var pagamento = new Pagamento
            {
                IdPagamento = id == Guid.Empty ? Guid.NewGuid() : id,
                IdAgendamento = idAgendamento,
                IdMeioPagamento = idMeioPagamento,

                Tipo = Request.Form["tipo"],
                Valor = valor,
                Data = data,

                Contos = Request.Form["contos"],
                Campos = Request.Form["campos"],
                
                Ativo = true,
                Status = "Pago", // Cadastro manual assume Pago? Ou Pendente? Mantendo coerente.
                Moeda = "BRL",
                CriadoEm = DateTime.UtcNow
            };

            var handler = new PagamentoHandler();

            if (id != Guid.Empty)
            {
                handler.Atualizar(pagamento);
                Mensagem = "Pagamento atualizado com sucesso!";
            }
            else
            {
                handler.CadastrarPagamento(pagamento);
                id = pagamento.IdPagamento;
                Mensagem = "Pagamento cadastrado com sucesso!";
            }

            OnGet(id != Guid.Empty ? id : (Guid?)null);
        }
    }
}
