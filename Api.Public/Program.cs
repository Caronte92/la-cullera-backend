// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using System.Threading.RateLimiting;
using Api.Public.Middleware;
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

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddInfrastructureServices(builder.Configuration);

// FluentValidation (auto-validation)
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
        ValidateIssuer = true,
        ValidIssuer = "la-cullera-api",
        ValidateAudience = true,
        ValidAudience = "la-cullera-client",
      };
    });

builder.Services.AddAuthorization();

// Add policies
builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("AdminOnly", policy =>
  {
    policy.RequireAuthenticatedUser();
    policy.RequireRole("Admin");
  });
});

// Swagger / OpenAPI with JWT support
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Api.Public", Version = "v1" });
  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JWT Authorization header using the Bearer scheme.",
  });
  c.AddSecurityRequirement(new OpenApiSecurityRequirement
  {
    {
      new OpenApiSecurityScheme
      {
        Reference = new OpenApiReference
        {
          Type = ReferenceType.SecurityScheme,
          Id = "Bearer",
        },
      },
      Array.Empty<string>()
    },
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

  // Global limiter
  options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
      RateLimitPartition.GetFixedWindowLimiter(
          partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
          factory: partition => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = true,
            PermitLimit = 100,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(15),
          }));

  // Login specific limiter
  options.AddPolicy("login", httpContext =>
      RateLimitPartition.GetFixedWindowLimiter(
          partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
          factory: partition => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = true,
            PermitLimit = 10,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1),
          }));
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();
app.UseRateLimiter(); // Enable Rate Limiting
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api.Public v1"));

  // Auto-create/update DB schema in development (no migrations needed)
  using var scope = app.Services.CreateScope();
  var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
  db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
  Predicate = _ => false,
}).WithName("Liveness");

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
  Predicate = check => check.Tags.Contains("ready"),
}).WithName("Readiness");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.MapControllers();

// Protected endpoint example
app.MapGet("/admin", () => Results.Ok(new { secret = "only for admins" }))
    .RequireAuthorization("AdminOnly")
    .WithName("AdminOnly");

app.Run();
