using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Doctorly.Scheduling.Application.Behaviours;

/// <summary>
/// Logs the name and duration of each request passing through the mediator.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Request type only - the payloads carry patient names and appointment times.
        var requestName = typeof(TRequest).Name;
        var timer = Stopwatch.StartNew();

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next().ConfigureAwait(false);

            timer.Stop();
            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                timer.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            timer.Stop();
            logger.LogWarning(
                ex,
                "{RequestName} failed after {ElapsedMilliseconds}ms",
                requestName,
                timer.ElapsedMilliseconds);

            throw;
        }
    }
}
