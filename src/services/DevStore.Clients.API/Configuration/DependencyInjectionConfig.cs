using DevStore.Clients.API.Data;
using DevStore.Clients.API.Data.Repository;
using DevStore.Clients.API.Models;
using DevStore.WebAPI.Core.Usuario;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DevStore.Clients.API.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAspNetUser, AspNetUser>();

            services.AddScoped<IClienteRepository, ClientRepository>();
            services.AddScoped<ClientContext>();
        }
    }
}