using gaihcc2dataserver.classes;
using gaihcc2dataserver;
using System.Collections.Concurrent;
using gaihcc2dataserver.common;
using TrendFileService.Services;

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
builder.Logging.AddFilter("Grpc", LogLevel.Warning);
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

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

// builder.WebHost.ConfigureKestrel((context, serverOptions) =>
// {
//     //local port
//     var serverPort = CommonUtilities.GetEnvVariableWithDefault(config.env.trend_port_anyip, config.app.trend_port_anyip);
//     if (!string.IsNullOrEmpty(serverPort))
//     {
//         serverOptions.ListenAnyIP(int.Parse(serverPort), o => o.Protocols = HttpProtocols.Http1AndHttp2);
//     }
// });

var app = builder.Build();
//
// Print Version
//
Logger.write(logLevel.info, "APICLIENT - Version: " + config.system.version);
//
// Setup Thread Pool
//
ThreadPool.SetMinThreads(config.parameters.threadpool_min_size, config.parameters.threadpool_min_size);
//
// Show current level
//
Logger.write(logLevel.info, $"Current Log Level is \"{ Logger.GetCurrentLevel()}\"");
//
// Start the ZMQ engine
//
Logger.write(logLevel.info,"Program()- Start APICLIENT client");

var zmqTask = new App(webhookQueue, cycleTimer, vars, config);

Task<int> tsk = Task.Run(() => {
    var task = zmqTask;
    task.Main();
    return 0;
    });
//
//initialize trend service load from environment variables if provided
//
TrendService.InitEnvironment(zmqTask.GAIBufferArray, config);
//
// Configure the HTTP request pipeline.
//
app.MapGrpcService<TrendService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");
//
// Configure the HTTP request pipeline.
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGrpcReflectionService();
}

app.UseHttpsRedirection();

app.UseAuthorization();

Logger.write(logLevel.info,"Program()- Map API Controllers");
app.MapControllers();
Logger.write(logLevel.info,"CLIENT Started!");

app.Run();
