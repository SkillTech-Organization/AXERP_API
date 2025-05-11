using AXERP.API.GoogleHelper;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddResiliencePipeline(GoogleSheetManagerFactory.PipelineName, builder =>
        {
            builder
                .AddRetry(SetRetry())
                .AddTimeout(SetTimeout());
        });

        return services;
    }

    private static RetryStrategyOptions SetRetry()
    {
        string? value = Environment.GetEnvironmentVariable(RetryCounter);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Parameter {RetryCounter} is missing");

        int retryAttempts = Convert.ToInt32(value);

        return new()
        {
            MaxRetryAttempts = retryAttempts,
        };
    }

    private static TimeoutStrategyOptions SetTimeout()
    {
        string? value = Environment.GetEnvironmentVariable("GSTimeoutHandlerInSeconds");

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Parameter {TimeoutHandlerInSeconds} is missing");

        int timeout = Convert.ToInt32(value);

        return new()
        {
            Timeout = TimeSpan.FromSeconds(timeout),
        };
    }
}
