using MassTransit;
using Microsoft.AspNetCore.Http;
using RtuItLab.Infrastructure.MassTransit;
using RtuItLab.Infrastructure.Models.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RtuItLab.Infrastructure.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IBus _bus;
        private readonly Uri _rabbitMqUri = new Uri("rabbitmq://rabbit/identityQueue");

        // BUG FIX: was injecting IBusControl via Invoke() parameter.
        // IBusControl is the bus lifecycle manager (Start/Stop); it must NOT
        // be injected into per-request middleware. IBus is the correct interface
        // for sending/publishing messages from application code. Also moved
        // injection to the constructor (Singleton middleware — constructor
        // injection is the right place for Singleton dependencies).
        public JwtMiddleware(RequestDelegate next, IBus bus)
        {
            _next = next;
            _bus  = bus;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"]
                .FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await AttachUserToContext(context, token);

            await _next(context);
        }

        private async Task AttachUserToContext(HttpContext context, string token)
        {
            try
            {
                var client = _bus.CreateRequestClient<TokenRequest>(
                    _rabbitMqUri, TimeSpan.FromSeconds(10));

                var response = await client.GetResponse<User>(
                    new TokenRequest { Token = token });

                if (response.Message != null)
                    context.Items["User"] = response.Message;
            }
            catch
            {
                // Invalid or expired token — do not set User; protected
                // endpoints will return 401 via the [Authorize] filter.
            }
        }
    }
}
