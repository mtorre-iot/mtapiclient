using Microsoft.Extensions.Configuration;

namespace mtapiclient.classes;

public class LoadConfiguration
{
        public static IConfiguration LoadConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        return builder.Build();
    }
}
