using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CorteCor.Models;
using CorteCor.Handlers;
using System.Threading.Tasks;
using System.Collections.Generic;
using CorteCor.Services;
using System.Data.SqlClient;
using System;

namespace CorteCor.Pages.Configuracoes
{
    public class IndexModel : PageModel
    {
        private readonly IntegracaoHandler _integracaoHandler;
        private readonly FinanceiroHandler _financeiroHandler;
        private readonly SalaoConfigFiscalHandler _fiscalHandler;
        private readonly MeioPagamentoHandler _meioPagamentoHandler;
        private readonly ICriptografiaService _criptoService;
        private readonly SalaoHandler _salaoHandler;

        public IndexModel(IntegracaoHandler integracaoHandler, FinanceiroHandler financeiroHandler, SalaoConfigFiscalHandler fiscalHandler, MeioPagamentoHandler meioPagamentoHandler, SalaoHandler salaoHandler, ICriptografiaService criptoService)
        {
            _integracaoHandler = integracaoHandler;
            _financeiroHandler = financeiroHandler;
            _fiscalHandler = fiscalHandler;
            _meioPagamentoHandler = meioPagamentoHandler;
            _salaoHandler = salaoHandler;
            _criptoService = criptoService;
        }

        private int GetIdSalao()
        {
            int.TryParse(User.FindFirst("IdSalao")?.Value, out int id);
            return id;
        }

        [BindProperty]
        public ConfigGeral ConfigGeral { get; set; } = new();

        [BindProperty]
        public SalaoConfigFiscal ConfigFiscal { get; set; } = new();

        [BindProperty]
        public ConfigPix ConfigPix { get; set; } = new();

        [BindProperty]
        public IFormFile? CertificadoFile { get; set; }

        [BindProperty]
        public Salao Salao { get; set; } = new();

        public List<PlanoContas> PlanoContas { get; set; } = new();
        public List<ContaCaixa> ContasCaixa { get; set; } = new();
        public List<ConfigApi> Apis { get; set; } = new();
        public List<MeioPagamento> MeiosPagamento { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            int idSalao = GetIdSalao();
            if (idSalao == 0) return RedirectToPage("/Index");

            ConfigGeral = await _integracaoHandler.ObterConfigGeralAsync(idSalao) ?? new ConfigGeral { IdSalao = idSalao };
            ConfigFiscal = await _fiscalHandler.ObterPorSalaoAsync(idSalao) ?? new SalaoConfigFiscal { IdSalao = idSalao };
            ConfigPix = await _integracaoHandler.ObterConfigPixAsync(idSalao) ?? new ConfigPix { IdSalao = idSalao };
            Salao = _salaoHandler.Listar().Find(s => s.IdSalao == idSalao) ?? new Salao { IdSalao = idSalao };
            
            PlanoContas = await _financeiroHandler.ListarPlanoContasAsync(idSalao);
            ContasCaixa = await _financeiroHandler.ListarContasCaixaAsync(idSalao);
            Apis = await _integracaoHandler.ListarApisAsync(idSalao);
            MeiosPagamento = _meioPagamentoHandler.ListarPorSalao(idSalao, null);

            return Page();
        }

        public async Task<IActionResult> OnPostGeneralAsync()
        {
            ConfigGeral.IdSalao = GetIdSalao();
            await _integracaoHandler.SalvarConfigGeralAsync(ConfigGeral);
            TempData["Mensagem"] = "Configurações de operação salvas com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostFiscalAsync()
        {
            try 
            {
                int idSalao = GetIdSalao();
                ConfigFiscal.IdSalao = idSalao;

                // Sincronizar dados básicos da Unidade (Salao) para o Fiscal, removendo redundância na UI
                var salaoAtual = _salaoHandler.Listar().Find(s => s.IdSalao == idSalao);
                if (salaoAtual != null)
                {
                    if (string.IsNullOrEmpty(ConfigFiscal.Cnpj)) ConfigFiscal.Cnpj = salaoAtual.CNPJ;
                    if (string.IsNullOrEmpty(ConfigFiscal.RazaoSocial)) ConfigFiscal.RazaoSocial = salaoAtual.Nome;
                    
                    // Se o endereço fiscal estiver vazio, podemos sugerir ou copiar do salão (opcional, mas evita erros se o DB exigir)
                    if (string.IsNullOrEmpty(ConfigFiscal.EnderecoLogradouro)) ConfigFiscal.EnderecoLogradouro = salaoAtual.Endereco;
                }

                if (string.IsNullOrEmpty(ConfigFiscal.Cnpj))
                {
                    TempData["MensagemErro"] = "O CNPJ da Unidade deve estar preenchido nos Dados da Unidade para salvar as configurações fiscais.";
                    return RedirectToPage();
                }

                // Se um arquivo de certificado foi enviado
                if (CertificadoFile != null && CertificadoFile.Length > 0)
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        await CertificadoFile.CopyToAsync(ms);
                        ConfigFiscal.CertificadoPfx = ms.ToArray();
                        ConfigFiscal.CertificadoBase64 = System.Convert.ToBase64String(ConfigFiscal.CertificadoPfx);
                    }
                }

                // Se uma nova senha foi fornecida, encriptar antes de salvar
                if (!string.IsNullOrEmpty(ConfigFiscal.CertificadoSenhaTexto))
                {
                    ConfigFiscal.CertificadoSenha = _criptoService.Criptografar(ConfigFiscal.CertificadoSenhaTexto);
                }

                await _fiscalHandler.SalvarAsync(ConfigFiscal);
                TempData["Mensagem"] = "Configurações fiscais salvas com sucesso!";
            }
            catch (System.Exception ex)
            {
                TempData["MensagemErro"] = "Ocorreu um erro ao salvar as configurações fiscais: " + ex.Message;
            }
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPlanoContasAsync([FromForm] PlanoContas plano)
        {
            plano.IdSalao = GetIdSalao();
            await _financeiroHandler.SavePlanoContasAsync(plano);
            TempData["Mensagem"] = "Plano de contas atualizado!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostContaCaixaAsync([FromForm] ContaCaixa conta)
        {
            conta.IdSalao = GetIdSalao();
            await _financeiroHandler.SaveContaCaixaAsync(conta);
            TempData["Mensagem"] = "Conta caixa/banco salva com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMeioPagamentoAsync([FromForm] MeioPagamento meio)
        {
            meio.IdSalao = GetIdSalao();
            if (meio.IdMeioPagamento == 0)
                _meioPagamentoHandler.CadastrarMeioPagamento(meio);
            else
                _meioPagamentoHandler.Atualizar(meio);

            TempData["Mensagem"] = "Meio de pagamento salvo com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPixAsync()
        {
            ConfigPix.IdSalao = GetIdSalao();
            await _integracaoHandler.SalvarConfigPixAsync(ConfigPix);
            TempData["Mensagem"] = "Configurações de Pix salvas!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApiAsync(string nomeApp)
        {
            var config = new ConfigApi
            {
                IdSalao = GetIdSalao(),
                NomeApp = nomeApp,
                ApiKey = System.Guid.NewGuid(),
                DataCriacao = System.DateTime.Now,
                Ativo = true
            };
            await _integracaoHandler.SalvarConfigApiAsync(config);
            TempData["Mensagem"] = "Novo token de API gerado com sucesso!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostExcluirApiAsync(int idApi)
        {
            // Simplified: for now just toggle active
            // In real app, we might want a hard delete or real toggle
            return RedirectToPage();
        }

        public IActionResult OnPostSalaoAsync()
        {
            try 
            {
                int idSalao = GetIdSalao();
                Salao.IdSalao = idSalao;

                // Buscar dados atuais para não perder campos que não estão no form (como DataCadastro se o hidden falhar)
                // ou outros campos que o SalaoHandler exige e podem vir nulos do form
                var salaoAtual = _salaoHandler.Listar().Find(s => s.IdSalao == idSalao);
                if (salaoAtual != null)
                {
                    if (Salao.DataCadastro == DateTime.MinValue) Salao.DataCadastro = salaoAtual.DataCadastro;
                    if (string.IsNullOrEmpty(Salao.Status)) Salao.Status = salaoAtual.Status;
                }

                _salaoHandler.Atualizar(Salao);
                TempData["Mensagem"] = "Dados básicos da unidade atualizados!";
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                TempData["MensagemErro"] = "Já existe outra unidade cadastrada com este CNPJ (" + Salao.CNPJ + "). Não é permitido duplicar o CNPJ.";
            }
            catch (System.Exception ex)
            {
                TempData["MensagemErro"] = "Ocorreu um erro ao atualizar os dados da unidade: " + ex.Message;
            }
            
            return RedirectToPage();
        }
    }
}
