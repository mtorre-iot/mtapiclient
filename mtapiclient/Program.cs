using mtapiclient.classes;
using mtapiclient;
using System.Collections.Concurrent;
using mtapiclient.common;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json",
            optional: true,
            reloadOnChange: false);
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Program>();
    });

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Mvc", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Lifetime", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

var config = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();

Logger.AppName = config.system.appName;

Logger.Init(config.system.log_level);

// Add services to the container.

var vars = new Vars().Init(config);
var webhookQueue = new ConcurrentQueue<List<Record>>();
var cycleTimer = new CycleTimer();

builder.Services.AddSingleton(cycleTimer);
builder.Services.AddSingleton(vars);
builder.Services.AddSingleton(webhookQueue);
builder.Services.AddSingleton(config);
//
///builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(options => 
{
    options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
//
// Print Version
//
Logger.write(logLevel.info, "APICLIENT - Version: " + config.system.version);

Logger.write(logLevel.info,$"Webhook API Version: {config.system.version}");
//
// Setup Thread Pool
//
ThreadPool.SetMinThreads(config.parameters.threadpool_min_size, config.parameters.threadpool_min_size);
//
// Start the ZMQ engine
//
Logger.write(logLevel.info,"Program()- Start SDKAPI client");
Task<int> tsk = Task.Run(() => {
    var task = new App(webhookQueue, cycleTimer, vars, config);
    task.Main();
    return 0;
    });
//
// Configure the HTTP request pipeline.
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

Logger.write(logLevel.info,"Program()- Map API Controllers");
app.MapControllers();
Logger.write(logLevel.info,"CLIENT Started!");

app.Run();
