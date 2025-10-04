using Control.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IInterlockService, InMemoryInterlocks>();
builder.Services.AddSingleton<IJobService, InMemoryJobs>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<JobServiceHealthCheck>("job_service")
    .AddCheck<InterlockServiceHealthCheck>("interlock_service");

var app = builder.Build();

app.MapPost("/api/jobs/start", async (IJobService jobs, IInterlockService interlocks, [FromQuery] int speed) =>
{
    if (speed < 1 || speed > 1000)
        return Results.BadRequest(new { error = "invalid_speed", message = "Speed must be between 1 and 1000" });
    
    if (interlocks.IsAnyActive) 
        return Results.BadRequest(new { error = "interlock_active" });
    
    await jobs.StartAsync(speed);
    return Results.Ok(new { state = "Running", speed });
});

app.MapPost("/api/jobs/stop", async (IJobService jobs) =>
{
    await jobs.StopAsync();
    return Results.Ok(new { state = "Idle" });
});

app.MapGet("/api/status", (InMemoryJobs jobs) => Results.Ok(new { state = jobs.State, speed = jobs.Speed }));

// Health check endpoints
app.MapHealthChecks("/health");

app.Run();

// Make Program class accessible for testing
public partial class Program { }

public sealed class InMemoryInterlocks : IInterlockService
{
    public bool IsAnyActive { get; private set; }
    public void SetActive(bool active) => IsAnyActive = active;
}

public sealed class InMemoryJobs : IJobService
{
    public string State { get; private set; } = "Idle";
    public int Speed { get; private set; } = 0;

    public Task StartAsync(int speed) { State = "Running"; Speed = speed; return Task.CompletedTask; }
    public Task StopAsync() { State = "Idle"; Speed = 0; return Task.CompletedTask; }
}

public class JobServiceHealthCheck : IHealthCheck
{
    private readonly IJobService _jobService;

    public JobServiceHealthCheck(IJobService jobService)
    {
        _jobService = jobService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - service is healthy if it can be accessed
            var state = _jobService is InMemoryJobs jobs ? jobs.State : "Unknown";
            return Task.FromResult(HealthCheckResult.Healthy($"Job service is operational. Current state: {state}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Job service is not responding", ex));
        }
    }
}

public class InterlockServiceHealthCheck : IHealthCheck
{
    private readonly IInterlockService _interlockService;

    public InterlockServiceHealthCheck(IInterlockService interlockService)
    {
        _interlockService = interlockService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isActive = _interlockService.IsAnyActive;
            var status = isActive ? "Active" : "Inactive";
            return Task.FromResult(HealthCheckResult.Healthy($"Interlock service is operational. Status: {status}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Interlock service is not responding", ex));
        }
    }
}
