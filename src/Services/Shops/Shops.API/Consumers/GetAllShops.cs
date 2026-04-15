using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using RtuItLab.Infrastructure.MassTransit.Shops.Requests;
using RtuItLab.Infrastructure.Models.Shops;
using Shops.Domain.Services;

namespace Shops.API.Consumers
{
    public class GetAllShops : ShopsBaseConsumer, IConsumer<GetAllShopsRequest>
    {
        public GetAllShops(IShopsService shopsService) : base(shopsService)
        {
        }

        public async Task Consume(ConsumeContext<GetAllShopsRequest> context)
        {
            // ShopsService.GetAllShops() returns Task<ICollection<Shop>> —
            // await is mandatory here. Without it RespondAsync gets a Task
            // object instead of ICollection<Shop> and the build fails with
            // CS1061 'ICollection<Shop> does not contain GetAwaiter'.
            ICollection<Shop> shops = await ShopsService.GetAllShops();
            await context.RespondAsync(shops);
        }
    }
}
