using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Configuracoes
{
    public class IndexModel : PageModel
    {
        private readonly SalaoConfigFiscalHandler _fiscalHandler;
        private readonly ICriptografiaService _criptoService;
        private readonly SalaoHandler _salaoHandler;

        public IndexModel(
            SalaoConfigFiscalHandler fiscalHandler,
            SalaoHandler salaoHandler,
            ICriptografiaService criptoService)
        {
            _fiscalHandler = fiscalHandler;
            _salaoHandler = salaoHandler;
            _criptoService = criptoService;
        }

        private int GetIdSalao()
        {
            int.TryParse(User.FindFirst("IdSalao")?.Value, out int id);
            return id;
        }

        [BindProperty]
        public SalaoConfigFiscal ConfigFiscal { get; set; } = new();

        [BindProperty]
        public IFormFile? CertificadoFile { get; set; }

        [BindProperty]
        public Salao Salao { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            int idSalao = GetIdSalao();
            if (idSalao == 0)
            {
                return RedirectToPage("/Index");
            }

            ConfigFiscal = await _fiscalHandler.ObterPorSalaoAsync(idSalao) ?? new SalaoConfigFiscal { IdSalao = idSalao };
            Salao = _salaoHandler.Listar().Find(s => s.IdSalao == idSalao) ?? new Salao { IdSalao = idSalao };

            return Page();
        }

        public async Task<IActionResult> OnPostFiscalAsync()
        {
            try
            {
                int idSalao = GetIdSalao();
                var fiscalAtual = await _fiscalHandler.ObterPorSalaoAsync(idSalao) ?? new SalaoConfigFiscal { IdSalao = idSalao };

                ConfigFiscal.IdSalao = idSalao;
                ConfigFiscal.IdConfigFiscal = fiscalAtual.IdConfigFiscal;

                // Preserva campos técnicos que não ficam expostos na tela simplificada.
                ConfigFiscal.CodigoMunicipioIBGE = fiscalAtual.CodigoMunicipioIBGE;
                ConfigFiscal.CodigoUFIBGE = fiscalAtual.CodigoUFIBGE;
                ConfigFiscal.RegimeEspecialTributacao = fiscalAtual.RegimeEspecialTributacao;
                ConfigFiscal.IssExigibilidade = fiscalAtual.IssExigibilidade == 0 ? 1 : fiscalAtual.IssExigibilidade;
                ConfigFiscal.IssRetido = fiscalAtual.IssRetido == 0 ? 2 : fiscalAtual.IssRetido;
                ConfigFiscal.EnderecoNumero = fiscalAtual.EnderecoNumero;
                ConfigFiscal.EnderecoBairro = fiscalAtual.EnderecoBairro;
                ConfigFiscal.EnderecoCep = fiscalAtual.EnderecoCep;
                ConfigFiscal.EnderecoCidade = fiscalAtual.EnderecoCidade;
                ConfigFiscal.EnderecoUF = fiscalAtual.EnderecoUF;
                ConfigFiscal.Telefone = fiscalAtual.Telefone;
                ConfigFiscal.Email = fiscalAtual.Email;
                ConfigFiscal.CertificadoPfx = fiscalAtual.CertificadoPfx;
                ConfigFiscal.CertificadoBase64 = fiscalAtual.CertificadoBase64;
                ConfigFiscal.CertificadoValidade = fiscalAtual.CertificadoValidade;

                if (fiscalAtual.CertificadoSenha is { Length: > 0 } && string.IsNullOrWhiteSpace(ConfigFiscal.CertificadoSenhaTexto))
                {
                    ConfigFiscal.CertificadoSenha = fiscalAtual.CertificadoSenha;
                }

                var salaoAtual = _salaoHandler.Listar().Find(s => s.IdSalao == idSalao);
                if (salaoAtual != null)
                {
                    if (string.IsNullOrWhiteSpace(ConfigFiscal.Cnpj))
                    {
                        ConfigFiscal.Cnpj = salaoAtual.CNPJ;
                    }

                    if (string.IsNullOrWhiteSpace(ConfigFiscal.RazaoSocial))
                    {
                        ConfigFiscal.RazaoSocial = salaoAtual.Nome;
                    }

                    if (string.IsNullOrWhiteSpace(ConfigFiscal.EnderecoLogradouro))
                    {
                        ConfigFiscal.EnderecoLogradouro = salaoAtual.Endereco;
                    }
                }

                if (string.IsNullOrWhiteSpace(ConfigFiscal.Cnpj))
                {
                    TempData["MensagemErro"] = "O CNPJ da empresa deve estar preenchido em Dados da Empresa antes de salvar as configurações fiscais.";
                    return RedirectToPage();
                }

                if (CertificadoFile != null && CertificadoFile.Length > 0)
                {
                    using var ms = new System.IO.MemoryStream();
                    await CertificadoFile.CopyToAsync(ms);
                    ConfigFiscal.CertificadoPfx = ms.ToArray();
                    ConfigFiscal.CertificadoBase64 = Convert.ToBase64String(ConfigFiscal.CertificadoPfx);
                }

                if (!string.IsNullOrWhiteSpace(ConfigFiscal.CertificadoSenhaTexto))
                {
                    ConfigFiscal.CertificadoSenha = _criptoService.Criptografar(ConfigFiscal.CertificadoSenhaTexto);
                }

                await _fiscalHandler.SalvarAsync(ConfigFiscal);
                TempData["Mensagem"] = "Configurações fiscais salvas com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = "Ocorreu um erro ao salvar as configurações fiscais: " + ex.Message;
            }

            return RedirectToPage();
        }

        public IActionResult OnPostSalaoAsync()
        {
            try
            {
                int idSalao = GetIdSalao();
                Salao.IdSalao = idSalao;

                var salaoAtual = _salaoHandler.Listar().Find(s => s.IdSalao == idSalao);
                if (salaoAtual != null)
                {
                    if (Salao.DataCadastro == DateTime.MinValue)
                    {
                        Salao.DataCadastro = salaoAtual.DataCadastro;
                    }

                    if (string.IsNullOrWhiteSpace(Salao.Status))
                    {
                        Salao.Status = salaoAtual.Status;
                    }
                }

                _salaoHandler.Atualizar(Salao);
                TempData["Mensagem"] = "Dados da empresa atualizados com sucesso!";
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                TempData["MensagemErro"] = "Já existe outra unidade cadastrada com este CNPJ (" + Salao.CNPJ + "). Não é permitido duplicar o CNPJ.";
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = "Ocorreu um erro ao atualizar os dados da empresa: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
