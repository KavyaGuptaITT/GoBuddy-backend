using System.Net;
using System.Text.Json;

namespace GoBuddy.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
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

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError;

            if (ex is ApplicationException)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
            }
            else if (ex is UnauthorizedAccessException)
            {
                statusCode = (int)HttpStatusCode.Unauthorized;
            }
            else if (ex is KeyNotFoundException)
            {
                statusCode = (int)HttpStatusCode.NotFound;
            }

            context.Response.StatusCode = statusCode;

            var result = new
            {
                success = false,
                statusCode = statusCode,
                message = ex.Message
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}
