// <copyright file="ErrorHandlingMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Infrastructure.Middleware;

public class ErrorHandlingMiddleware
{
  private readonly RequestDelegate next;

  public ErrorHandlingMiddleware(RequestDelegate next)
  {
    this.next = next;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await this.next(context);
    }
    catch (Exception ex)
    {
      Log.Error(ex, "Unhandled exception");
      await HandleExceptionAsync(context, ex);
    }
  }

  private static Task HandleExceptionAsync(HttpContext context, Exception exception)
  {
    var problem = new
    {
      title = "An unexpected error occurred.",
      detail = exception.Message,
      status = (int)HttpStatusCode.InternalServerError,
      instance = context.Request.Path,
    };

    var payload = JsonSerializer.Serialize(problem);
    context.Response.ContentType = "application/problem+json";
    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
    return context.Response.WriteAsync(payload);
  }
}

public static class ErrorHandlingMiddlewareExtensions
{
  public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
  {
    return app.UseMiddleware<ErrorHandlingMiddleware>();
  }
}
