using MassTransit;
using Microsoft.AspNetCore.Mvc;
using RtuItLab.Infrastructure.Filters;
using RtuItLab.Infrastructure.MassTransit;
using RtuItLab.Infrastructure.Models;
using RtuItLab.Infrastructure.Models.Identity;
using System.Threading.Tasks;

namespace Identity.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IRequestClient<AuthenticateRequest> _authClient;
        private readonly IRequestClient<RegisterRequest>     _registerClient;

        public AccountController(
            IRequestClient<AuthenticateRequest> authClient,
            IRequestClient<RegisterRequest>     registerClient)
        {
            _authClient     = authClient;
            _registerClient = registerClient;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthenticateRequest model)
        {
            var response = await _authClient.GetResponse<AuthenticateResponse>(model);
            return Ok(ApiResult<AuthenticateResponse>.Success200(response.Message));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var response = await _registerClient.GetResponse<RegisterResponse>(model);
            return Ok(ApiResult<RegisterResponse>.Success200(response.Message));
        }

        [HttpGet("user")]
        [Authorize]
        public IActionResult GetUser()
        {
            var user = HttpContext.Items["User"] as User;
            return Ok(ApiResult<User>.Success200(user));
        }
    }
}
