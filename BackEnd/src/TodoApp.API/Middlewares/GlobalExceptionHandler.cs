using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Business;
using TodoApp.Business.Common.Exceptions;

namespace TodoApp.API.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		var (status, title) = exception switch
		{
			ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
			InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "ایمیل یا رمز عبور نادرست است."),
			UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "احراز هویت لازم است یا توکن نامعتبر است"),
			ForbiddenAccessException => (StatusCodes.Status403Forbidden, exception.Message),
			NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
			ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
			ConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
			LockedException => (StatusCodes.Status423Locked, "Account locked"),
			_ => (StatusCodes.Status500InternalServerError, "Server error")
		};

		if (status == 500)
		{
			logger.LogError(exception, "Unhandled exception");
		}
		else if (exception is InvalidCredentialsException or LockedException)
		{
			logger.LogWarning("Auth failure {ExceptionType} on {Path}",
				exception.GetType().Name, httpContext.Request.Path);
		}

		var problem = new ProblemDetails
		{
			Status = status,
			Title = title,
			Instance = httpContext.Request.Path
		};

		if (exception is ConcurrencyException or ConflictException
					  or InvalidCredentialsException or LockedException)
		{
			problem.Detail = exception.Message;
		}

		if (exception is ValidationException ve)
		{
			problem.Extensions["errors"] = ve.Errors
				.GroupBy(e => e.PropertyName)
				.ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
		}

		httpContext.Response.StatusCode = status;
		await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
		return true;
	}
}
