using Robot.Service;
using Robot.Service.Framework;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "RoboStock Chatbot";
    })
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        AppSettings options = configuration.GetSection("AppSettings").Get<AppSettings>();

        services.AddSingleton(options);

        services.AddHttpClient();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
