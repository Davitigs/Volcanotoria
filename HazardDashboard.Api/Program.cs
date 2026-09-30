using HazardDashboard.Api.Data;
using HazardDashboard.Api.Jobs;
using HazardDashboard.Api.Hubs;
using HazardDashboard.Api.Contracts;
using HazardDashboard.Api.Validation;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<HazardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("HazardDatabase"), npgsql => npgsql.UseNetTopologySuite()));
// Controllers receive one context per request; background jobs create their own context from the factory.
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<HazardDbContext>>().CreateDbContext());
builder.Services.AddHttpClient("usgs", client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHangfire(configuration => configuration
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HazardDatabase"))));
builder.Services.AddHangfireServer();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins("http://localhost:4200", "http://localhost:8080", "http://localhost:80")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<VolcanoBoundsQueryValidator>();

var app = builder.Build();

app.UseCors("web");

await using (var scope = app.Services.CreateAsyncScope())
{
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<HazardDbContext>(), app.Environment, CancellationToken.None);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<HazardHub>("/hubs/hazard");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire"); // Add authorization filters here before production exposure.
}

RecurringJob.AddOrUpdate<UsGeologicalSurveyIngestionJob>(
    "usgs-earthquakes-all-day",
    job => job.RunAsync(CancellationToken.None),
    "*/10 * * * *");

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
