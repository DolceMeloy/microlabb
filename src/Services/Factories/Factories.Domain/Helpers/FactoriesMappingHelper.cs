using Factories.DAL.ContextModels;
using RtuItLab.Infrastructure.Models.Shops;

namespace Factories.Domain.Helpers
{
    public static class FactoriesMappingHelper
    {
        public static ProductByFactory ToProductByFactoryDto(this ProductByFactoryContext context) =>
            new ProductByFactory
            {
                ShopId    = context.ShopId,
                ProductId = context.ProductId,
                Count     = (int)context.Count
            };
    }
}
