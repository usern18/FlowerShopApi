
using System.Net;
using System.Text.Json;
using FlowerShopApi.Exceptions;

namespace FlowerShopApi.Middleware
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
            catch (OperationCanceledException)
                when (context.RequestAborted.IsCancellationRequested)
            {
                // Клієнт скасував запит.
                // Не намагаємося надсилати відповідь.
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
            catch (InternalServerException)
            {
                await WriteErrorResponse(
                    context,
                    HttpStatusCode.InternalServerError,
                    "Внутрішня помилка сервера");
            }
            catch (Exception)
            {
                await WriteErrorResponse(
                    context,
                    HttpStatusCode.InternalServerError,
                    "Непередбачена помилка сервера");
            }
        }

        private static async Task WriteErrorResponse(
            HttpContext context,
            HttpStatusCode statusCode,
            string message)
        {
            if (context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message
            };

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(
                json,
                context.RequestAborted);
        }
    }
}
