using FileUtilityLib.Core.Compatibility;
using FileUtilityLib.Core.Interfaces;
using FileUtilityLib.Core.Services;
using FileUtilityLib.Scheduler.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileUtilityLib.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Agrega FileUtilityLib a la colección de servicios
        /// </summary>
        public static IServiceCollection AddFileUtilityLib(this IServiceCollection services, string? configDirectory = null)
        {
            /*// Agregar logging si no está configurado
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });*/

            // Configurar logging según el framework
            ConfigureLoggingForFramework(services);

            // Registrar el servicio principal
            services.AddSingleton<IFileUtilityService>(provider =>
                new FileUtilityService(provider, configDirectory));

            return services;
        }

        /*/// <summary>
        /// Crea una instancia standalone de FileUtilityService
        /// </summary>
        public static FileUtilityService CreateFileUtilityService(string? configDirectory = null)
        {
            var services = new ServiceCollection();
            services.AddFileUtilityLib(configDirectory);

            var serviceProvider = services.BuildServiceProvider();
            return (FileUtilityService)serviceProvider.GetRequiredService<IFileUtilityService>();
        }*/

        /// <summary>
        /// Crea una instancia standalone de FileUtilityService
        /// </summary>
        public static FileUtilityService CreateFileUtilityService(string? configDirectory = null)
        {
            var services = new ServiceCollection();

            // Configurar logging según el framework
            ConfigureLoggingForFramework(services);

            var serviceProvider = services.BuildServiceProvider();
            return new FileUtilityService(serviceProvider, configDirectory);
        }

        /// <summary>
        /// Configura logging compatible con el framework actual
        /// </summary>
        private static void ConfigureLoggingForFramework(IServiceCollection services)
        {
#if NET6_0_OR_GREATER || NET7_0_OR_GREATER || NET8_0_OR_GREATER
            // .NET 6+ - Configuración moderna
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
#elif NETSTANDARD2_1
            // .NET Standard 2.1
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });
#else
            // .NET Framework y .NET Standard 2.0 - Configuración compatible
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
#endif
        }

        /// <summary>
        /// Obtiene información sobre la compatibilidad del framework
        /// </summary>
        public static void ShowFrameworkInfo()
        {
            var framework = FrameworkCompatibility.GetFrameworkInfo();
            var supportsModern = FrameworkCompatibility.SupportsModernFeatures();

            Console.WriteLine($"FileUtilityLib ejecutándose en: {framework}");
            Console.WriteLine($"Características modernas: {(supportsModern ? "Disponibles" : "Limitadas")}");
        }
    }
}
