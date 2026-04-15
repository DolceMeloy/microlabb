using MassTransit;
using Purchases.Domain.Services;
using RtuItLab.Infrastructure.MassTransit.Purchases.Requests;
using System.Threading.Tasks;

namespace Purchases.API.Consumers
{
    public class AddTransaction : PurchasesBaseConsumer, IConsumer<AddTransactionRequest>
    {
        public AddTransaction(IPurchasesService purchasesService) : base(purchasesService)
        {
        }

        public async Task Consume(ConsumeContext<AddTransactionRequest> context)
        {
            // FIX: Shops использует Send (fire-and-forget), а не Request/Response.
            // RespondAsync выбрасывал исключение "No response address" —
            // сообщение уходило в error queue, транзакция в БД не создавалась.
            // Просто выполняем AddTransaction без попытки ответить отправителю.
            await PurchasesService.AddTransaction(context.Message.User, context.Message.Transaction);
        }
    }
}
