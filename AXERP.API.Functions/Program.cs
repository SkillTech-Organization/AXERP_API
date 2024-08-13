using AXERP.API.AppInsightsHelper.Managers;
using AXERP.API.Business.Commands;
using AXERP.API.Business.SheetProcessors;
using AXERP.API.Domain.AutoMapperProfiles;
using AXERP.API.Persistence.Factories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog.Sinks.MSSqlServer.Sinks.MSSqlServer.Options;


// Solution for sql collection disposed exception.
// Solution: https://github.com/Azure/azure-functions-dotnet-worker/issues/2397#issuecomment-2059233262
// TODO: alternate solution
#pragma warning disable AZFW0014 // Missing expected registration of ASP.NET Core Integration services
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging((hostingContext, logging) =>
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Worker", LogEventLevel.Warning)
            .MinimumLevel.Override("Host", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .MinimumLevel.Override("Function", LogEventLevel.Debug)
            .MinimumLevel.Override("Azure.Storage", LogEventLevel.Error)
            .MinimumLevel.Override("Azure.Core", LogEventLevel.Error)
            .MinimumLevel.Override("Azure.Identity", LogEventLevel.Error)
            .Enrich.WithProperty("Application", "AXERP.API")
            .Enrich.FromLogContext()
            .WriteTo.Console(LogEventLevel.Debug)
            .WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)
            .WriteTo.MSSqlServer(Environment.GetEnvironmentVariable("SqlConnectionString"), new MSSqlServerSinkOptions
            {
                AutoCreateSqlDatabase = true,
                AutoCreateSqlTable = true,
                TableName = "LogEvents",
                UseSqlBulkCopy = true
            })
            .Filter.ByIncludingOnly(x => x.Properties["SourceContext"].ToString().StartsWith("AXERP.API"))
            .CreateLogger();

        logging.AddSerilog(Log.Logger, true);
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddAutoMapper(typeof(ModelProfile));
        services.AddTransient<GasTransactionSheetProcessor>();
        services.AddTransient<UnitOfWorkFactory>();
        services.AddTransient<InsertTransactionsCommand>();
        services.AddTransient<UpdateReferencesByBlobFilesCommand>();
        services.AddTransient<DeleteTransactionsCommand>();
        services.AddTransient<AppInsightsManager>();
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
