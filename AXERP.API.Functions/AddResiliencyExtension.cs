using AXERP.API.GoogleHelper;
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
            var logger = context.ServiceProvider.GetRequiredService<ILogger>();

            builder
                .AddRetry(SetRetry(context.ServiceProvider))
                .AddTimeout(SetTimeout(context.ServiceProvider));
        });

        return services;
    }

    private static RetryStrategyOptions SetRetry(IServiceProvider serviceProvider)
    {
        string? value = Environment.GetEnvironmentVariable(RetryCounter);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Parameter {RetryCounter} is missing");

        int retryAttempts = Convert.ToInt32(value);

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
        string? value = Environment.GetEnvironmentVariable("GSTimeoutHandlerInSeconds");

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Parameter {TimeoutHandlerInSeconds} is missing");

        int timeout = Convert.ToInt32(value);

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
