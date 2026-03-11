using System.Threading.Tasks;
using CorteCor.Models;

namespace CorteCor.Services
{
    public interface IValidaParametrosMunicipioService
    {
        Task ValidateAsync(SalaoConfigFiscal config, CorteCor.Models.Servico servico);
    }
}
