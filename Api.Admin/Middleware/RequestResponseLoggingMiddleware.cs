// <copyright file="RequestResponseLoggingMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Admin.Middleware;

public class RequestResponseLoggingMiddleware
{
  private readonly RequestDelegate next;
  private readonly ILogger<RequestResponseLoggingMiddleware> logger;

  public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
  {
    this.next = next;
    this.logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await this.next(context);
      sw.Stop();
      this.logger.LogInformation(
          "{Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
          context.Request.Method,
          context.Request.Path,
          context.Response.StatusCode,
          sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
      sw.Stop();
      this.logger.LogError(ex, "{Method} {Path} failed after {ElapsedMilliseconds}ms",
          context.Request.Method,
          context.Request.Path,
          sw.ElapsedMilliseconds);
      throw;
    }
  }
}
