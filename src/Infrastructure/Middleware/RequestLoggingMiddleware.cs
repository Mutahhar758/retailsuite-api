using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;

namespace Retailer.Infrastructure.Middleware;

public class RequestLoggingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        LogContext.PushProperty("RequestTimeUTC", DateTime.UtcNow);
        string requestBody = string.Empty;
        if (httpContext.Request.Path.ToString().Contains("tokens"))
        {
            requestBody = "[Redacted] Contains Sensitive Information.";
        }
        else if (httpContext.Request.Path.ToString().Contains("jobs"))
        {
            await next(httpContext);
        }
        else
        {
            var request = httpContext.Request;

            if (!string.IsNullOrEmpty(request.ContentType)
                && request.ContentType.StartsWith("application/json"))
            {
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 4096, true);
                requestBody = await reader.ReadToEndAsync();

                // rewind for next middleware.
                request.Body.Position = 0;
            }
            else if (!string.IsNullOrEmpty(request.ContentType)
                && request.ContentType.StartsWith("multipart/form-data"))
            {
                Dictionary<string, string> formData = new Dictionary<string, string>();

                foreach (var formField in request.Form)
                {
                    formData.Add(formField.Key, formField.Value!);
                }

                requestBody = JsonConvert.SerializeObject(formData);
            }
        }

        LogContext.PushProperty("RequestBody", requestBody);
        Log.ForContext("RequestHeaders", httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), destructureObjects: true)
           .ForContext("RequestBody", requestBody)
           .Information("HTTP {RequestMethod} Request sent to {RequestPath}", httpContext.Request.Method, httpContext.Request.Path);
        await next(httpContext);
    }
}