using System.Reflection;

var basePath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
var startupLog = Path.Combine(basePath ?? ".", "logs", "startup.txt");
Directory.CreateDirectory(Path.GetDirectoryName(startupLog)!);

try
{
    File.AppendAllText(startupLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Program Start [args={string.Join(", ", args)}]{Environment.NewLine}");
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.CreateUmbracoBuilder()
        .AddBackOffice()
        .AddWebsite()
        .AddDeliveryApi()
        .AddComposers()
        .Build();

    WebApplication app = builder.Build();

    await app.BootUmbracoAsync();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        File.AppendAllText(startupLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Application Started {Environment.NewLine}");
    });

    app.Lifetime.ApplicationStopping.Register(() =>
    {
        File.AppendAllText(startupLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Application Stopping {Environment.NewLine}");
    });

    app.Lifetime.ApplicationStopped.Register(() =>
    {
        File.AppendAllText(startupLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Application Stopped {Environment.NewLine}");
    });

#if (UseHttpsRedirect)
app.UseHttpsRedirection();
#endif

    app.UseUmbraco()
        .WithMiddleware(u =>
        {
            u.UseBackOffice();
            u.UseWebsite();
        })
        .WithEndpoints(u =>
        {
            u.UseInstallerEndpoints();
            u.UseBackOfficeEndpoints();
            u.UseWebsiteEndpoints();
        });

    await app.RunAsync();

    File.AppendAllText(startupLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Program Stop{Environment.NewLine}");
}
catch (Exception e)
{
    File.AppendAllText(startupLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERR] {e}{Environment.NewLine}");
}
