using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Core.Services;
using FileUtilityLib.Scheduler.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileUtilityLib.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileUtilityLib(this IServiceCollection services, string? configDirectory = null)
        {
            // Agregar logging si no está configurado
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Registrar el servicio principal
            services.AddSingleton<IFileUtilityService>(provider =>
                new FileUtilityService(provider, configDirectory));

            return services;
        }

        public static FileUtilityService CreateFileUtilityService(string? configDirectory = null)
        {
            var services = new ServiceCollection();
            services.AddFileUtilityLib(configDirectory);

            var serviceProvider = services.BuildServiceProvider();
            return (FileUtilityService)serviceProvider.GetRequiredService<IFileUtilityService>();
        }
    }
}
