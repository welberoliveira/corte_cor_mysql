using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class FinanceiroResumoModel : PageModel
    {
        public List<ResumoCategoriaDTO> ResumoProdutos { get; set; } = new();
        public List<ResumoCategoriaDTO> ResumoServicos { get; set; } = new();

        public decimal TotalVendaProdutos => ResumoProdutos.Sum(r => r.TotalVenda);
        public decimal TotalCustoProdutos => ResumoProdutos.Sum(r => r.TotalCusto);
        public decimal MargemProdutos => TotalVendaProdutos - TotalCustoProdutos;

        public decimal TotalVendaServicos => ResumoServicos.Sum(r => r.TotalVenda);
        public decimal TotalCustoServicos => ResumoServicos.Sum(r => r.TotalCusto);
        public decimal MargemServicos => TotalVendaServicos - TotalCustoServicos;

        public void OnGet()
        {
            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
            
            var catHandler = new CategoriaProdutoHandler();
            var categorias = catHandler.ListarPorSalao(idSalao) ?? new List<CategoriaProduto>();

            var prodHandler = new ProdutoHandler();
            var produtos = prodHandler.ListarPorSalao(idSalao) ?? new List<Produto>();

            var servHandler = new ServicoHandler();
            var servicos = servHandler.ListarPorSalao(idSalao) ?? new List<Servico>();

            // Agrupar Produtos por Categoria
            ResumoProdutos = produtos
                .GroupBy(p => p.IdCategoria)
                .Select(g => new ResumoCategoriaDTO
                {
                    IdCategoria = g.Key ?? 0,
                    NomeCategoria = categorias.FirstOrDefault(c => c.IdCategoria == g.Key)?.Nome ?? "Sem Categoria",
                    Quantidade = g.Count(),
                    TotalVenda = g.Sum(p => p.PrecoVenda),
                    TotalCusto = g.Sum(p => p.PrecoCusto ?? 0)
                })
                .OrderByDescending(r => r.TotalVenda)
                .ToList();

            // Agrupar Serviços por Categoria
            ResumoServicos = servicos
                .GroupBy(s => s.IdCategoria)
                .Select(g => new ResumoCategoriaDTO
                {
                    IdCategoria = g.Key ?? 0,
                    NomeCategoria = categorias.FirstOrDefault(c => c.IdCategoria == g.Key)?.Nome ?? "Sem Categoria",
                    Quantidade = g.Count(),
                    TotalVenda = g.Sum(s => s.Preco),
                    TotalCusto = g.Sum(s => s.PrecoCusto ?? 0)
                })
                .OrderByDescending(r => r.TotalVenda)
                .ToList();
        }
    }

    public class ResumoCategoriaDTO
    {
        public int IdCategoria { get; set; }
        public string NomeCategoria { get; set; }
        public int Quantidade { get; set; }
        public decimal TotalVenda { get; set; }
        public decimal TotalCusto { get; set; }
        public decimal Margem => TotalVenda - TotalCusto;
        public decimal PercentualMargem => TotalVenda > 0 ? (Margem / TotalVenda) * 100 : 0;
    }
}
