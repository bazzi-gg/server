using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using System.Globalization;
using Server;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("ko-kr");

CreateHostBuilder(args).Build().Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddEnvironmentVariables(prefix: "APP_");
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
#if !DEBUG
                    webBuilder.UseSentry();
#endif
        });