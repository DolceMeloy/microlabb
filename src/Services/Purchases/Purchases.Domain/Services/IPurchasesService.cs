using RtuItLab.Infrastructure.MassTransit;
using RtuItLab.Infrastructure.Models.Identity;
using RtuItLab.Infrastructure.Models.Purchases;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Purchases.Domain.Services
{
    public interface IPurchasesService
    {
        Task<Transaction> GetTransactionById(User user, int id);
        Task<BaseResponseMassTransit> AddTransaction(User user, Transaction transaction);
        Task<BaseResponseMassTransit> AddShopTransaction(User user, Transaction transaction);
        Task<BaseResponseMassTransit> UpdateTransaction(User user, UpdateTransaction transaction);
        Task<ICollection<Transaction>> GetTransactions(User user);
    }
}
