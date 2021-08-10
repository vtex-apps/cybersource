using Cybersource.Models;
using System.Threading.Tasks;

namespace Cybersource.Services
{
    public interface IVtexApiService
    {
        Task<PickupPoints> ListPickupPoints();
        Task<TaxFallbackResponse> GetFallbackRate(string country, string postalCode, string provider = "avalara");
    }
}