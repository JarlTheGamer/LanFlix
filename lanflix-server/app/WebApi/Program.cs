using Lanflix.WebApi.Extensions;
using Lanflix.WebApi.Helpers;
using Serilog;

// Extract embedded config files on first run (Minecraft-style)
EmbeddedResourceExtractor.ExtractConfigFiles();

var builder = WebApplication.CreateBuilder(args);

// Add persistent configuration file (survives updates)
var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
var configDir = Path.Combine(baseDir, "config");
Directory.CreateDirectory(configDir);
builder.Configuration.AddJsonFile(Path.Combine(configDir, "lanflix.json"), optional: true, reloadOnChange: true);

// Configure Kestrel for HTTP/2 and HTTP/3 support
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Enable HTTP/2
    serverOptions.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
    });
    
    // Configure limits for optimal performance
    serverOptions.Limits.MaxConcurrentConnections = 1000;
    serverOptions.Limits.MaxConcurrentUpgradedConnections = 1000;
    serverOptions.Limits.MaxRequestBodySize = 2_147_483_648; // 2GB for large file uploads
    serverOptions.Limits.MinRequestBodyDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(
        bytesPerSecond: 100,
        gracePeriod: TimeSpan.FromSeconds(10));
    serverOptions.Limits.MinResponseDataRate = new Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate(
        bytesPerSecond: 100,
        gracePeriod: TimeSpan.FromSeconds(10));
    
    // HTTP/2 specific settings
    serverOptions.Limits.Http2.MaxStreamsPerConnection = 100;
    serverOptions.Limits.Http2.HeaderTableSize = 4096;
    serverOptions.Limits.Http2.MaxFrameSize = 16384;
    serverOptions.Limits.Http2.MaxRequestHeaderFieldSize = 8192;
    serverOptions.Limits.Http2.InitialConnectionWindowSize = 131072;
    serverOptions.Limits.Http2.InitialStreamWindowSize = 98304;
    
    // Keep-alive settings
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Configure Serilog with structured logging and sensitive data redaction
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "Lanflix.Server")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.With<Lanflix.Infrastructure.Logging.SensitiveDataRedactionEnricher>()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddLanflixServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseLanflixPipeline();
app.UseLanflixStaticFiles(builder.Configuration);
app.UseLanflixAuth();
app.MapLanflixEndpoints();

try
{
    // Display ASCII art banner
    var banner = @"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║   ██╗      █████╗ ███╗   ██╗███████╗██╗     ██╗██╗  ██╗       ║
║   ██║     ██╔══██╗████╗  ██║██╔════╝██║     ██║╚██╗██╔╝       ║
║   ██║     ███████║██╔██╗ ██║█████╗  ██║     ██║ ╚███╔╝        ║
║   ██║     ██╔══██║██║╚██╗██║██╔══╝  ██║     ██║ ██╔██╗        ║
║   ███████╗██║  ██║██║ ╚████║██║     ███████╗██║██╔╝ ██╗       ║
║   ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝╚═╝     ╚══════╝╚═╝╚═╝  ╚═╝       ║
║                                                               ║
║                    Media Streaming Server                     ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
";
    
    Console.WriteLine(banner);
    Log.Information("Starting Lanflix Server");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    
    // Initialize database and media folders
    await app.InitializeLanflixDatabaseAsync();
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class accessible to integration tests
public partial class Program { }

