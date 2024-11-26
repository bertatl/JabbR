using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace JabbR.Middleware
{
    public class RequireHttpsHandler
    {
        private readonly RequestDelegate _next;

        public RequireHttpsHandler(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (!request.IsHttps)
            {
                var builder = new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1, request.Path, request.QueryString.ToString());
                builder.Scheme = "https";

                response.Headers["Location"] = builder.ToString();
                response.StatusCode = 302;

                return;
            }
            else
            {
                await _next(context);
            }
        }
    }
}