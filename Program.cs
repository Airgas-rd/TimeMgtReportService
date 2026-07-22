using Serilog;
using TimeMgtReportService;
using TimeMgtReportService.Helpers;
using TimeMgtReportService.Interfaces;
using TimeMgtReportService.Models;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json").Build();

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices(services =>
    {
        //services.AddHostedService<Worker>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddHostedService<ReportService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddSingleton<EmailSettings>();
        services.AddOptions();
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"))
            .AddSingleton<EmailSettings>();
    }).UseSerilog()
    .Build();

Helper.Initialize(configuration);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(configuration["Logging:Logpath"] ?? string.Empty)
    .CreateLogger();
host.Run();
