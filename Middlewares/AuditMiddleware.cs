using System.Security.Claims;
using CrudCloud.api.Services;

namespace CrudCloud.api.Middlewares;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        // Solo auditar POST/PUT/DELETE
        if (context.Request.Method is "POST" or "PUT" or "DELETE")
        {
            var userId = int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id : 0;

            var action = context.Request.Method;
            var entity = context.Request.Path.Value?.Trim('/');
            var detail = $"[{action}] {entity}";

            await auditService.LogAsync(userId, action, entity ?? "unknown", detail);
        }

        await _next(context);
    }
}