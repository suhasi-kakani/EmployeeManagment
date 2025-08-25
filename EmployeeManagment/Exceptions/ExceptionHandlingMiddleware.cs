using System.Net;
using Microsoft.Azure.Cosmos;

namespace EmployeeManagment.Exceptions
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            this.logger = logger;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (GlobalException ex)
            {
                logger.LogWarning(ex, "Handled application exception");
                await HandleExceptionAsync(context, ex.StatusCode, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Resource not found");
                await HandleExceptionAsync(context, (int)HttpStatusCode.NotFound, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Unauthorized access");
                await HandleExceptionAsync(context, (int)HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (CosmosException ex)
            {
                logger.LogWarning(ex, "Internal server error");
                await HandleExceptionAsync(context, (int)HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled server error");
                await HandleExceptionAsync(context, (int)HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. Please try again later.");
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;

            var errorResponse = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    }
}
