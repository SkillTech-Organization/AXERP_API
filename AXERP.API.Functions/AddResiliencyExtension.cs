using AXERP.API.GoogleHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace AXERP.API.Functions;

public static class AddResiliencyExtension
{
    private const string RetryCounter = "GSRetryCounter";
    private const string TimeoutHandlerInSeconds = "GSTimeoutHandlerInSeconds";

    public static IServiceCollection AddResiliency(this IServiceCollection services)
    {
        services.AddResiliencePipeline(GoogleSheetManagerFactory.PipelineName, (builder, context) =>
        {
            builder
                .AddRetry(SetRetry(context.ServiceProvider))
                .AddTimeout(SetTimeout(context.ServiceProvider));
        });

        return services;
    }

    private static RetryStrategyOptions SetRetry(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        int retryAttempts = configuration.GetValue<int>(RetryCounter);

        var logger = serviceProvider.GetRequiredService<ILogger<RetryStrategyOptions>>();

        return new()
        {
            MaxRetryAttempts = retryAttempts,
            OnRetry = args =>
            {
                logger.LogWarning("Retry. Attempt: {0}", args.AttemptNumber);
                return ValueTask.CompletedTask;
            },
        };
    }

    private static TimeoutStrategyOptions SetTimeout(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        int timeout = configuration.GetValue<int>(TimeoutHandlerInSeconds);

        var logger = serviceProvider.GetRequiredService<ILogger<TimeoutStrategyOptions>>();

        return new()
        {
            Timeout = TimeSpan.FromSeconds(timeout),
            OnTimeout = args => 
            {
                logger.LogWarning("Request timed our.");
                return ValueTask.CompletedTask;
            }
        };
    }
}
