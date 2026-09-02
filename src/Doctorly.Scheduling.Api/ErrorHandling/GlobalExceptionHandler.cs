using Doctorly.Scheduling.Application.Common.Exceptions;
using Doctorly.Scheduling.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Doctorly.Scheduling.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblem(validationException),
            ConcurrencyConflictException conflict => CreateConcurrencyProblem(conflict),
            DomainException domainException => CreateDomainProblem(domainException),
            _ => CreateUnexpectedProblem(),
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        }).ConfigureAwait(false);
    }

    private static ProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        };

        problem.Extensions["errors"] = errors;

        return problem;
    }

    private static ProblemDetails CreateConcurrencyProblem(ConcurrencyConflictException exception) => new()
    {
        Status = StatusCodes.Status412PreconditionFailed,
        Title = "The event was changed by someone else.",
        Detail = exception.Message,
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.13",
    };

    private static ProblemDetails CreateDomainProblem(DomainException exception) => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "The request breaks a scheduling rule.",
        Detail = exception.Message,
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    };

    // No exception detail: this response can reach an external caller.
    private static ProblemDetails CreateUnexpectedProblem() => new()
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "An unexpected error occurred.",
        Detail = "The request could not be completed. Please contact support if this persists.",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    };
}
