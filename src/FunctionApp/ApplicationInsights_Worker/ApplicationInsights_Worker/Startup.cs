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
        /// DI Config
        /// </summary>
        /// <param name="builder"></param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;
            var provider = services.BuildServiceProvider();
            var configuration = provider.GetRequiredService<IConfiguration>();

            var conStorageString = Environment.GetEnvironmentVariable("STORAGE_CONNECT_STRING");

            // サービスのDI設定を行う
            services.AddTransient<IStorageProvider, StorageProvider>();
            services.AddTransient<IApplicationInsightsProvider, ApplicationInsightsProvider>();
            services.AddHttpClient<IApplicationInsightsProvider, ApplicationInsightsProvider>().
                SetHandlerLifetime(TimeSpan.FromMinutes(5));

            services.AddTransient<ITelemetryService, TelemetryService>();

            services.AddAzureClients(builder =>
            {
                // Blob設定
                builder.AddBlobServiceClient(conStorageString)
                .ConfigureOptions(options => {
                    options.Retry.Mode = Azure.Core.RetryMode.Exponential;
                    options.Retry.MaxRetries = 5;
                    options.Retry.MaxDelay = TimeSpan.FromSeconds(120);
                });

                // File設定
                builder.AddFileServiceClient(conStorageString)
                .ConfigureOptions(options =>
                {
                    options.Retry.Mode = Azure.Core.RetryMode.Exponential;
                    options.Retry.MaxRetries = 5;
                    options.Retry.MaxDelay = TimeSpan.FromSeconds(120);
                });
            });

        }

    }
}
