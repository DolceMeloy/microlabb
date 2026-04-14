using RtuItLab.Infrastructure.Models.Shops;
using Shops.DAL.ContextModels;

namespace Shops.Domain.Helpers
{
    public static class ShopsMappingHelper
    {
        public static Shop ToShopDto(this ShopContext context) => new Shop
        {
            Id          = context.Id,
            Address     = context.Address,
            PhoneNumber = context.PhoneNumber
        };

        public static Product ToProductDto(this ProductContext context) => new Product
        {
            ProductId = context.Id,
            Name      = context.Name,
            Category  = context.Category,
            Cost      = context.Cost,
            Count     = context.Count
        };

        public static ProductByReceiptContext ToProductByReceiptContext(this Product product) =>
            new ProductByReceiptContext
            {
                Name     = product.Name,
                Category = product.Category,
                Cost     = product.Cost,
                Count    = product.Count
            };
    }
}
