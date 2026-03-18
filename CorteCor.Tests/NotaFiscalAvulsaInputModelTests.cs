using System.ComponentModel.DataAnnotations;
using CorteCor.Pages.Fiscal;
using Xunit;

namespace CorteCor.Tests
{
    public class NotaFiscalAvulsaInputModelTests
    {
        [Fact]
        public void InputModel_DeveAceitarConfiguracaoBasicaValida()
        {
            var model = CriarInputValido();

            var resultados = Validar(model);

            Assert.Empty(resultados);
        }

        [Fact]
        public void InputModel_DeveExigirUmItemParaEmissao()
        {
            var model = CriarInputValido();
            model.Itens.Clear();

            var resultados = Validar(model);

            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("ao menos um item", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void InputModel_DeveRejeitarMultiplosItensParaNfse()
        {
            var model = CriarInputValido();
            model.Modelo = "NFSE";
            model.Itens.Add(new NotaFiscalAvulsaModel.NotaFiscalAvulsaItem
            {
                XProd = "Servico adicional",
                qCom = 1,
                vUnCom = 10,
                CodigoTributacao = "060101"
            });

            var resultados = Validar(model);

            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("um item", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void InputModel_DeveValidarCodigoTributacaoParaNfse()
        {
            var model = CriarInputValido();
            model.Itens[0].CodigoTributacao = null;

            var resultados = Validar(model);

            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("codigo de tributacao", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void InputModel_DeveRejeitarEmailInvalido()
        {
            var model = CriarInputValido();
            model.DestinatarioEmail = "email-invalido";

            var resultados = Validar(model);

            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("e-mail valido", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void InputModel_DeveRejeitarItemSemDescricaoOuQuantidadeInvalida()
        {
            var model = CriarInputValido();
            model.Itens[0].XProd = "";
            model.Itens[0].qCom = 0;

            var resultados = Validar(model);

            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("descricao do item", System.StringComparison.OrdinalIgnoreCase));
            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("quantidade", System.StringComparison.OrdinalIgnoreCase));
        }

        private static NotaFiscalAvulsaModel.InputModel CriarInputValido()
        {
            return new NotaFiscalAvulsaModel.InputModel
            {
                Ambiente = 2,
                Modelo = "NFSE",
                NaturezaOperacao = "Prestacao de servico",
                Serie = 1,
                Numero = 10,
                DataEmissao = System.DateTime.Now,
                EmitenteCnpj = "12345678000100",
                EmitenteNome = "Salao CorteCor",
                EmitenteCodMun = 3143302,
                DestinatarioCpfCnpj = "12345678901",
                DestinatarioNome = "Cliente Teste",
                DestinatarioCodMun = 3143302,
                DestinatarioEmail = "cliente@teste.com",
                Itens =
                {
                    new NotaFiscalAvulsaModel.NotaFiscalAvulsaItem
                    {
                        XProd = "Servico avulso",
                        qCom = 1,
                        vUnCom = 50,
                        CodigoTributacao = "060101",
                        AliquotaISS = 5
                    }
                }
            };
        }

        private static System.Collections.Generic.List<ValidationResult> Validar(object model)
        {
            var resultados = new System.Collections.Generic.List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), resultados, validateAllProperties: true);
            return resultados;
        }
    }
}
