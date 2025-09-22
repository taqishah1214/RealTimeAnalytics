using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using RealtimeAnalytics.Api.Hubs;
using RealtimeAnalytics.Api.Services;
using RealtimeAnalytics.Api.Services.Abstractions;
using RealtimeAnalytics.Infrastructure;
using RealtimeAnalytics.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

// Ensure EF Core picks migrations from the Infrastructure assembly explicitly
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var connStr = builder.Configuration.GetConnectionString("Default") ?? "Data Source=mydb.sqlite;";
    // Use the simple assembly name so EF can correctly load migrations
    var migrationsAssembly = typeof(AppDbContext).Assembly.GetName().Name;
    opt.UseSqlite(connStr, b => b.MigrationsAssembly(migrationsAssembly));
});



builder.Services.AddSingleton<ITimeSeriesStore, TimeSeriesStore>();
builder.Services.AddSingleton(Channel.CreateBounded<RealtimeAnalytics.Domain.SensorReading>(new BoundedChannelOptions(1_000_000)
{
    SingleReader = false, SingleWriter = false, FullMode = BoundedChannelFullMode.DropOldest
}));
builder.Services.AddScoped<AlertEvaluator>();
builder.Services.AddHostedService<SensorIngestionSimulator>();
builder.Services.AddHostedService<ReadingProcessor>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SensorSeed.EnsureSeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.Run();
