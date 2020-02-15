using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketChat
{
    public static class WebSocketChatExtensions
    {
        public static IServiceCollection AddWebSocketChatHandler(this IServiceCollection services)
        {
            var exportedTypes = Assembly.GetEntryAssembly()?.ExportedTypes;
            if (exportedTypes == null) return services;

            foreach (var type in exportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
                {
                    services.AddSingleton(type);
                }
            }

            return services;
        }

        public static IApplicationBuilder MapWebSocketChatMiddleware(this IApplicationBuilder app, PathString path,
            WebSocketHandler handler)
        {
            return app.Map(path, _app => _app.UseMiddleware<WebSocketChatMiddleware>(handler));
        }
    }
}