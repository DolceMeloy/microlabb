using MassTransit;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RtuItLab.Infrastructure.Exceptions;
using RtuItLab.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RtuItLab.Infrastructure.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int statusCode;
            string message;

            switch (exception)
            {
                case BadRequestException _:
                    statusCode = 400;
                    message = exception.Message;
                    break;
                case UnauthorizedException _:
                    statusCode = 401;
                    message = exception.Message;
                    break;
                case ForbiddenException _:
                    statusCode = 403;
                    message = exception.Message;
                    break;
                case NotFoundException _:
                    statusCode = 404;
                    message = exception.Message;
                    break;
                case RequestFaultException rfe:
                    statusCode = 500;
                    message = rfe.Message;
                    break;
                default:
                    statusCode = 500;
                    message = exception.Message;
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var result = JsonConvert.SerializeObject(
                ApiResult<object>.Failure(statusCode, new List<string> { message }));
            return context.Response.WriteAsync(result);
        }
    }
}
