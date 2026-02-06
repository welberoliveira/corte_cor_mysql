using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    public class Agendamentos2Model : PageModel
    {
        // DEMO em memória (troque por SQL Server depois)
        private static readonly ConcurrentDictionary<string, CalendarEvent> _events = new();

        public List<Servico> Servicos { get; set; } = new();
        public List<Pessoa> Clientes { get; set; } = new();


        public void OnGet()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var servicoHandler = new ServicoHandler();
            Servicos = servicoHandler.ListarPorSalao(idSalao);

            var pessoaHandler = new PessoaHandler();
            Clientes = pessoaHandler.ListarPorSalao(idSalao);

        }

        // FullCalendar espera: [{ id, title, start, end }]
        public IActionResult OnGetEvents(DateTime start, DateTime end)
        {
            var items = _events.Values
                .Where(e => e.Start < end && e.End > start)
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    start = e.Start,
                    end = e.End
                })
                .ToList();

            return new JsonResult(items);
        }

        public class CreateRequest
        {
            public string? Start { get; set; }
            public string? End { get; set; }
            public int IdPessoa { get; set; }
            public int IdServico { get; set; }
        }


        [ValidateAntiForgeryToken]
        public IActionResult OnPostCreate([FromBody] CreateRequest req)
        {
            if (req == null) return BadRequest();

            if (string.IsNullOrWhiteSpace(req.Start)) return BadRequest("Start inválido");
            if (!DateTime.TryParse(req.Start, out var start)) return BadRequest("Start inválido");

            if (req.IdServico <= 0) return BadRequest("Serviço é obrigatório");
            if (req.IdPessoa <= 0) return BadRequest("Cliente é obrigatório");

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var servicoHandler = new ServicoHandler();
            var servico = servicoHandler.ObterPorId(req.IdServico);
            if (servico == null) return BadRequest("Serviço não encontrado");

            var pessoaHandler = new PessoaHandler();
            var pessoa = pessoaHandler.ObterPorId(req.IdPessoa);
            if (pessoa == null) return BadRequest("Cliente não encontrado");
            if (pessoa.IdSalao != idSalao) return BadRequest("Cliente inválido para este salão");

            // FIM calculado pela duração do serviço
            //var end = start.Add(servico.Duracao);
            DateTime end;

            if (!string.IsNullOrWhiteSpace(req.End) && DateTime.TryParse(req.End, out var endParsed) && endParsed > start)
                end = endParsed;
            else
                end = start.Add(servico.Duracao);


            var id = Guid.NewGuid().ToString("N");
            var title = $"{servico.Nome} - {pessoa.Nome}";

            _events[id] = new CalendarEvent
            {
                Id = id,
                Title = title,
                Start = start,
                End = end,
                IdServico = req.IdServico
            };

            return new JsonResult(new
            {
                id,
                servicoNome = servico.Nome,
                servicoCor = servico.Cor
            });
        }


        [ValidateAntiForgeryToken]
        public IActionResult OnPostDelete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            _events.TryRemove(id, out _);
            // TODO: aqui seria DELETE no banco
            return new JsonResult(new { ok = true });
        }

        private class CalendarEvent
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            // (opcional)
            public int IdServico { get; set; }
        }
    }
}
