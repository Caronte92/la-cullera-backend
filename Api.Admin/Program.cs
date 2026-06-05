// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using System.Threading.RateLimiting;
using Api.Admin.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// Bootstrap Serilog early to capture startup logs
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext().WriteTo.Console());

builder.Services.AddOpenApi();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddFluentValidationAutoValidation();

// Register validators from Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateUserValidator>();

// Authentication - JWT Bearer
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET is not configured. Set it in environment variables or appsettings.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
      options.SaveToken = true;
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
      };
    });

builder.Services.AddAuthorization();

// Swagger / OpenAPI with JWT support
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api.Admin", Version = "v1" });
  var securityScheme = new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JWT Authorization header using the Bearer scheme.",
  };
  c.AddSecurityDefinition("Bearer", securityScheme);
  c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() },
    });
});

// CORS
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", policy =>
  {
    policy.WithOrigins(builder.Configuration["Cors:Origin"] ?? "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
  });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

  options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
      RateLimitPartition.GetFixedWindowLimiter(
          partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
          factory: partition => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = true,
            PermitLimit = 200, // Higher limit for admin
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(15),
          }));
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Api.Admin v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () =>
{
  return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
})
.WithName("Health")
.WithOpenApi();

// Admin Endpoints
var adminGroup = app.MapGroup("/admin").RequireAuthorization("AdminOnly").WithOpenApi();

adminGroup.MapGet("/", () => Results.Ok(new { secret = "only for admins" }))
    .WithName("AdminOnly");

app.Run();
