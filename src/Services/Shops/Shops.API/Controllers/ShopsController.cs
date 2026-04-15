using Microsoft.AspNetCore.Mvc;
using RtuItLab.Infrastructure.Filters;
using RtuItLab.Infrastructure.Models;
using RtuItLab.Infrastructure.Models.Identity;
using RtuItLab.Infrastructure.Models.Shops;
using Shops.Domain.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shops.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopsController : ControllerBase
    {
        private readonly IShopsService _shopsService;

        public ShopsController(IShopsService shopsService)
        {
            _shopsService = shopsService;
        }

        [HttpGet]
        public IActionResult GetAllShops()
        {
            var shops = _shopsService.GetAllShops();
            return Ok(ApiResult<ICollection<Shop>>.Success200(shops));
        }

        [HttpGet("{shopId}")]
        public async Task<IActionResult> GetProducts(int shopId)
        {
            var products = await _shopsService.GetProductsByShop(shopId);
            return Ok(ApiResult<ICollection<Product>>.Success200(products));
        }

        [HttpPost("{shopId}/find_by_category")]
        public async Task<IActionResult> GetProductsByCategory(int shopId, [FromBody] Category category)
        {
            if (!ModelState.IsValid) return BadRequest();
            var products = await _shopsService.GetProductsByCategory(shopId, category.CategoryName);
            return Ok(ApiResult<ICollection<Product>>.Success200(products));
        }

        [Authorize]
        [HttpPost("{shopId}/order")]
        public async Task<IActionResult> BuyProducts(int shopId, [FromBody] ICollection<Product> products)
        {
            if (!ModelState.IsValid) return BadRequest();

            var user = HttpContext.Items["User"] as User;
            if (user == null)
                return Unauthorized(ApiResult<object>.Failure(401,
                    new List<string> { "Unauthorized" }));

            var result = await _shopsService.BuyProducts(shopId, products);
            return Ok(ApiResult<ICollection<Product>>.Success200(result));
        }
    }
}
