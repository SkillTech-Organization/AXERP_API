using AXERP.API.Domain.AutoMapperProfiles;
using AXERP.API.Functions.SheetProcessors;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


// Solution for sql collection disposed exception.
// Solution: https://github.com/Azure/azure-functions-dotnet-worker/issues/2397#issuecomment-2059233262
// TODO: alternate solution
#pragma warning disable AZFW0014 // Missing expected registration of ASP.NET Core Integration services
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddAutoMapper(typeof(ModelProfile));
        services.AddTransient<GasTransactionSheetProcessor>();
    })
    // Source: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#application-insights
    // Quote: "However, by default, the Application Insights SDK adds a logging filter that instructs the logger to capture only warnings and more severe logs. If you want to disable this behavior, remove the filter rule as part of service configuration"
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();
#pragma warning restore AZFW0014 // Missing expected registration of ASP.NET Core Integration services

host.Run();
