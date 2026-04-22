using Alerto.Application.Common.Exceptions;
using Alerto.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Alerto.Api.Middlewares;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Se produjo una excepción no controlada. TraceId={TraceId}", context.TraceIdentifier);

        ProblemDetails problemDetails = exception switch
        {
            ValidationException validationException => BuildValidationProblem(context, validationException),
            NotFoundException notFoundException => BuildProblem(context, StatusCodes.Status404NotFound, "Resource not found", notFoundException.Message),
            ConflictException conflictException => BuildProblem(context, StatusCodes.Status409Conflict, "Concurrency conflict", conflictException.Message),
            ExternalDependencyException externalDependencyException => BuildProblem(context, StatusCodes.Status503ServiceUnavailable, "External dependency unavailable", externalDependencyException.Message),
            DomainException domainException => BuildProblem(context, StatusCodes.Status422UnprocessableEntity, "Business rule violation", domainException.Message),
            UnauthorizedAccessException unauthorizedAccessException => BuildProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized", unauthorizedAccessException.Message),
            _ => BuildProblem(context, StatusCodes.Status500InternalServerError, "Unexpected error", "Se produjo un error inesperado.")
        };

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static ProblemDetails BuildProblem(HttpContext context, int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        }.WithTrace(context.TraceIdentifier);
    }

    private static ValidationProblemDetails BuildValidationProblem(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "Uno o más errores de validación impiden procesar la solicitud.",
            Instance = context.Request.Path
        }.WithTrace(context.TraceIdentifier);
    }
}

internal static class ProblemDetailsExtensions
{
    public static TProblemDetails WithTrace<TProblemDetails>(this TProblemDetails problemDetails, string traceId)
        where TProblemDetails : ProblemDetails
    {
        problemDetails.Extensions["traceId"] = traceId;
        return problemDetails;
    }
}
