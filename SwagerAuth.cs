using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Middleware.SwaggerAuth;

public class SwaggerAuthMiddleware
{
    private readonly RequestDelegate _next;
    public SwaggerAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            string? authHeader = context.Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic "))
            {
                var header = AuthenticationHeaderValue.Parse(authHeader);
                var parameter = header?.Parameter;

                if (!string.IsNullOrEmpty(parameter))
                {
                    var inBytes = Convert.FromBase64String(parameter);
                    var credentials = Encoding.UTF8.GetString(inBytes).Split(':');
                    var username = credentials[0];
                    var password = credentials[1];

                    if (username.Equals("swagger") && password.Equals("swagger"))
                    {
                        await _next.Invoke(context).ConfigureAwait(false);
                        return;
                    }
                }
            }

            context.Response.Headers.WWWAuthenticate = "Basic";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}

public static class SwaggerAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseSwaggerAuth(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SwaggerAuthMiddleware>();
    }
}