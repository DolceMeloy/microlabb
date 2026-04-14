using Purchases.DAL.ContextModels;
using RtuItLab.Infrastructure.Models.Purchases;
using RtuItLab.Infrastructure.Models.Shops;
using System.Linq;

namespace Purchases.Domain.Helpers
{
    // FIX: файл отсутствовал в репозитории — проект не компилировался.
    public static class PurchasesMappingHelper
    {
        public static Transaction ToTransactionDto(this TransactionContext ctx) => new Transaction
        {
            Id              = ctx.Id,
            Date            = ctx.Date,
            TransactionType = ctx.TransactionType,
            IsShopCreate    = ctx.IsShopCreate,
            Products        = ctx.Products?.Select(p => p.ToProductDto()).ToList(),
            Receipt         = ctx.Receipt == null ? null : new Receipt
            {
                ShopId = ctx.Receipt.ShopId,
                Cost   = ctx.Receipt.Cost,
                Count  = ctx.Receipt.Count,
                Date   = ctx.Receipt.Date
            }
        };

        public static TransactionContext ToTransactionContext(this Transaction t) => new TransactionContext
        {
            Date            = t.Date,
            TransactionType = t.TransactionType,
            IsShopCreate    = t.IsShopCreate,
            Products        = t.Products?.Select(p => p.ToProductContext()).ToList(),
            Receipt         = t.Receipt == null ? null : new ReceiptContext
            {
                ShopId = t.Receipt.ShopId,
                Cost   = t.Receipt.Cost,
                Count  = t.Receipt.Count,
                Date   = t.Receipt.Date
            }
        };

        public static Product ToProductDto(this ProductContext ctx) => new Product
        {
            ProductId = ctx.ProductId,
            Name      = ctx.Name,
            Category  = ctx.Category,
            Cost      = ctx.Cost,
            Count     = ctx.Count
        };

        public static ProductContext ToProductContext(this Product p) => new ProductContext
        {
            ProductId = p.ProductId,
            Name      = p.Name,
            Category  = p.Category,
            Cost      = p.Cost,
            Count     = p.Count
        };
    }
}
