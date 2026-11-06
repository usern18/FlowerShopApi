using System.Net;
using System.Text.Json;
using FlowerShopApi.Exceptions;

namespace FlowerShopApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BadRequestException ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (ForbiddenException ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.Forbidden, ex.Message);
            }
            catch (NotFoundException ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (InternalServerException ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Необроблений виняток під час {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await WriteErrorResponse(
                    context,
                    HttpStatusCode.InternalServerError,
                    "Непередбачена помилка сервера");
            }
        }

        private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = message
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }
}