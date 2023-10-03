using ApplicationInsights_Worker;
using ApplicationInsights_Worker.Repositories;
using ApplicationInsights_Worker.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Startup))]
namespace ApplicationInsights_Worker
{
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// This method is called to configure the DI container.
        /// </summary>
        /// <param name="builder">Azure Functions host builder</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;
            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IConfiguration>();

            var conStorageString = Environment.GetEnvironmentVariable("STORAGE_CONNECT_STRING");

            // Register dependencies
            services.AddScoped<IStorageProvider, StorageProvider>();
            services.AddScoped<IApplicationInsightsProvider, ApplicationInsightsProvider>();
            services.AddHttpClient<IApplicationInsightsProvider, ApplicationInsightsProvider>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
            services.AddScoped<ITelemetryService, TelemetryService>();

            // Configure Azure clients
            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(conStorageString)
                .ConfigureOptions(options =>
                {
                    // Set exponential retry policy for BlobServiceClient
                    options.Retry.Mode = Azure.Core.RetryMode.Exponential;
                    options.Retry.MaxRetries = 5;
                    options.Retry.MaxDelay = TimeSpan.FromSeconds(120);
                });

                // Add FileServiceClient
                builder.AddFileServiceClient(conStorageString)
                .ConfigureOptions(options =>
                {
                    // Set exponential retry policy for FileServiceClient
                    options.Retry.Mode = Azure.Core.RetryMode.Exponential;
                    options.Retry.MaxRetries = 5;
                    options.Retry.MaxDelay = TimeSpan.FromSeconds(120);
                });
            });

        }
    }
}
