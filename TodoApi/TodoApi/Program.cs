using System.Text;
using Microsoft.AspNetCore.HttpLogging;
using Serilog;
using Serilog.Settings.Configuration;

var builder = WebApplication.CreateBuilder(args);
var options = new ConfigurationReaderOptions { SectionName = "Servicelog" };

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration, options)
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .CreateLogger();

Log.Logger = logger;

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddHttpLogging(logging =>
{
    // Customize HTTP logging here.
    logging.LoggingFields = HttpLoggingFields.All;
    logging.MediaTypeOptions.AddText("application/json");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});
//builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
var app = builder.Build();
app.UseHttpLogging();
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.MapPost("/vf/security/originate", Handler);
app.Run("http://localhost:3000");

string Handler(HttpContext context)
{
    using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
    {
        //Thread.Sleep(10000);
        string jsonstring = reader.ReadToEndAsync().Result;
        logger.Information(jsonstring);
        return jsonstring;
    }
}