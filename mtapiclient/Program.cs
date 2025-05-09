using Serilog;
using mtapiclient.classes;
using mtapiclient;
using System.ComponentModel;
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

// Add services to the container.

var config = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
var vars = new Vars().Init(config);
var webhookQueue = new ConcurrentQueue<Record>();
var cycleTimer = new CycleTimer();

builder.Host.UseSerilog((context, logConfig) => logConfig
    .ReadFrom.Configuration(context.Configuration));

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
Serilog.ILogger logger = app.Services.GetService<Serilog.ILogger>();
//
// Print Version
//
logger.Information($"Webhook API Version: {config.system.version}");
//
// Setup Thread Pool
//
ThreadPool.SetMinThreads(config.parameters.threadpool_min_size, config.parameters.threadpool_min_size);
//
// Start the ZMQ engine
//
logger.Information("Program()- Start SDKAPI client");
Task<int> tsk = Task.Run(() => {
    var task = new App(logger, webhookQueue, cycleTimer, vars, config);
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

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

app.UseAuthorization();

logger.Information("Program()- Map API Controllers");
app.MapControllers();

logger.Information("Program()- Start API Engine");
logger.Information("Webhook API Started!");

app.Run();
