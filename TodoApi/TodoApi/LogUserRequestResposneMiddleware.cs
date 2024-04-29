
using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Settings.Configuration;

public class RequestResponseLoggingMiddleware
{
    static Logger _logger;
    private readonly RequestDelegate _next;
    static RequestResponseLoggingMiddleware()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var optionsReq = new ConfigurationReaderOptions { SectionName = "Requestlog" };
        _logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration, optionsReq)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .CreateLogger();
    }

    public RequestResponseLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }



    MemoryStream injectedRequestStream;

    public async Task Invoke(HttpContext context)
    {
        try
        {

            context.Request.EnableBuffering();
            await LogRequest(context);
            await LogResponse(context);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
        }
    }

    public async Task  LogRequest(HttpContext context)
    {
        var formString = await new StreamReader(context.Request.Body).ReadToEndAsync();
        injectedRequestStream = new MemoryStream();
        byte[] bytesToWrite = Encoding.UTF8.GetBytes(formString);
        injectedRequestStream.Write(bytesToWrite, 0, bytesToWrite.Length);
        injectedRequestStream.Seek(0, SeekOrigin.Begin);
        context.Request.Body = injectedRequestStream;
        _logger.Information(formString);

    }

    public async Task LogResponse(HttpContext context)
    {
        Stream originalBody = context.Response.Body;
        try
        {
            using (var memStream = new MemoryStream())
            {
                context.Response.Body = memStream;
                await _next(context);
                memStream.Position = 0;
                string responseBody = new StreamReader(memStream).ReadToEnd();
                _logger.Information(responseBody);
                memStream.Position = 0;
                await memStream.CopyToAsync(originalBody);
            }
        }
        catch
        {
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
            injectedRequestStream.Dispose();
        }
    }
}

public static class LogUserRequestResposneMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLog(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}