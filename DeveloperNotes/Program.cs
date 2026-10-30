using DeveloperNotes.Services;
using DeveloperNotes.Settings;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// redis, move this to dependency injection class when
// transitioning to clean architecture
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6397";

    var options = ConfigurationOptions.Parse(connectionString);
    options.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(options);
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<MongoDbService>();

builder.Services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

builder.Services.AddSingleton<GeohashService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/redis-test", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();

    var latency = await db.PingAsync();

    return Results.Ok(new
    {
        Message = "Hello Alnick! - Redis ;-;",
        Latency = latency.TotalMilliseconds
    });
});

app.Run();