using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class MeioPagamentoCadastroModel : PageModel
    {
        public MeioPagamento MeioPagamento { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            if (id.HasValue)
            {
                var handler = new MeioPagamentoHandler();
                MeioPagamento = handler.Listar().FirstOrDefault(p => p.IdMeioPagamento == id.Value);
                ButtonText = "Atualizar";
            }
        }

        private static bool GetBool(string key, Microsoft.AspNetCore.Http.IFormCollection form, bool defaultValue = false)
        {
            var v = form[key].ToString();
            if (string.IsNullOrWhiteSpace(v)) return defaultValue;
            return v == "true" || v == "on" || v == "1";
        }

        private static decimal ParseDecimalBR(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;
            valor = valor.Trim().Replace(".", "").Replace(",", ".");
            return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static byte? ParseByteNullable(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            if (byte.TryParse(valor, out var b)) return b;
            return null;
        }

        public void OnPost()
        {
            int id = 0;
            int.TryParse(Request.Form["id"], out id);

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            short prazoRecebimentoDias = 0;
            short.TryParse(Request.Form["prazoRecebimentoDias"], out prazoRecebimentoDias);

            DateTime dataCadastro = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(Request.Form["dataCadastro"]))
                dataCadastro = DateTime.Parse(Request.Form["dataCadastro"]);

            var form = Request.Form;

            bool permiteParcelamento = GetBool("permiteParcelamento", form, false);
            byte? parcelasMax = permiteParcelamento ? ParseByteNullable(form["parcelasMax"]) : null;

            var meio = new MeioPagamento
            {
                IdMeioPagamento = id,
                Nome = form["nome"],
                Tipo = form["tipo"],
                Gateway = form["gateway"],

                PermiteParcelamento = permiteParcelamento,
                ParcelasMax = parcelasMax,

                TaxaPercentual = ParseDecimalBR(form["taxaPercentual"]),
                TaxaFixa = ParseDecimalBR(form["taxaFixa"]),
                PrazoRecebimentoDias = prazoRecebimentoDias,

                Ativo = GetBool("ativo", form, true),

                IdSalao = idSalao,
                DataCadastro = dataCadastro,

                MpAccessTokenProd = form["mpAccessTokenProd"],
                MpAccessTokenSandbox = form["mpAccessTokenSandbox"],
                MpPublicKeyProd = form["mpPublicKeyProd"],
                MpPublicKeySandbox = form["mpPublicKeySandbox"],
                MpProduction = GetBool("mpProduction", form, false)
            };

            var handler = new MeioPagamentoHandler();

            if (id > 0)
            {
                handler.Atualizar(meio);
                Mensagem = "Meio de pagamento atualizado com sucesso!";
            }
            else
            {
                id = handler.CadastrarMeioPagamento(meio);
                Mensagem = "Meio de pagamento cadastrado com sucesso!";
            }

            OnGet(id > 0 ? id : (int?)null);
        }
    }
}

