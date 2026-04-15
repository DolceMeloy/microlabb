using RtuItLab.Infrastructure.Models.Shops;

namespace RtuItLab.Infrastructure.Models.Purchases
{
    public class Receipt
    {
        public int ShopId { get; set; }
        public decimal Cost { get; set; }
    }
}
