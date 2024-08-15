using AXERP.API.AppInsightsHelper.Managers;
using AXERP.API.Business.Commands;
using AXERP.API.Business.Queries;
using AXERP.API.Business.SheetProcessors;
using AXERP.API.Domain.AutoMapperProfiles;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;


// Solution for sql collection disposed exception.
// Solution: https://github.com/Azure/azure-functions-dotnet-worker/issues/2397#issuecomment-2059233262
// TODO: alternate solution
#pragma warning disable AZFW0014 // Missing expected registration of ASP.NET Core Integration services
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging((hostingContext, logging) =>
    {
        var colOpts = new ColumnOptions();

        colOpts.Store.Remove(StandardColumn.Message);
        colOpts.Store.Remove(StandardColumn.Properties);
        colOpts.Store.Remove(StandardColumn.MessageTemplate);
        colOpts.Store.Remove(StandardColumn.Exception);
        colOpts.Store.Remove(StandardColumn.Level);

        colOpts.TimeStamp.ColumnName = "When";

        colOpts.AdditionalColumns = new List<SqlColumn>
        {
            new SqlColumn
            {
                ColumnName = "ProcessId",
                DataType = System.Data.SqlDbType.BigInt
            },

            new SqlColumn
            {
                ColumnName = "Who",
                DataType = System.Data.SqlDbType.NVarChar,
                DataLength = 500
            },

            new SqlColumn
            {
                ColumnName = "Function",
                DataType = System.Data.SqlDbType.NVarChar,
                DataLength = 500
            },

            new SqlColumn
            {
                ColumnName = "System",
                DataType = System.Data.SqlDbType.NVarChar,
                DataLength = 500
            },

            new SqlColumn
            {
                ColumnName = "Description",
                DataType = System.Data.SqlDbType.NVarChar,
                DataLength = -1 // maximum allowed
            },

            new SqlColumn
            {
                ColumnName = "Result",
                DataType = System.Data.SqlDbType.NVarChar,
                DataLength = 100
            }
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Worker", LogEventLevel.Warning)
            .MinimumLevel.Override("Host", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .MinimumLevel.Override("Function", LogEventLevel.Debug)
            .MinimumLevel.Override("Azure.Storage", LogEventLevel.Error)
            .MinimumLevel.Override("Azure.Core", LogEventLevel.Error)
            .MinimumLevel.Override("Azure.Identity", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.Console(LogEventLevel.Debug)
            .WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)
            .WriteTo.MSSqlServer(Environment.GetEnvironmentVariable("SqlConnectionString"), new MSSqlServerSinkOptions
            {
                AutoCreateSqlDatabase = false,
                AutoCreateSqlTable = true,
                TableName = "LogEvents",
                UseSqlBulkCopy = true
            }, columnOptions: colOpts)
            //.Filter.ByIncludingOnly(x => x.Properties["SourceContext"].ToString().StartsWith("AXERP.API"))
            .Filter.ByIncludingOnly(x => x.MessageTemplate.Text.Contains("ProcessId"))
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
        services.AddTransient<AxerpLoggerFactory>();
        services.AddTransient<ListBlobFilesQuery>();
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
