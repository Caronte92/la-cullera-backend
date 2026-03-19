// <copyright file="ErrorHandlingMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Infrastructure.Middleware;

public class ErrorHandlingMiddleware
{
  private readonly RequestDelegate next;
  private readonly IWebHostEnvironment env;

  public ErrorHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env)
  {
    this.next = next;
    this.env = env;
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
      await this.HandleExceptionAsync(context, ex);
    }
  }

  private Task HandleExceptionAsync(HttpContext context, Exception exception)
  {
    var problem = new
    {
      title = "An unexpected error occurred.",
      detail = this.env.IsDevelopment() ? exception.Message : "An internal error has occurred.",
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
