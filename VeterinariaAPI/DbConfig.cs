namespace VeterinariaAPI;

public static class DbConfig
{
    private static readonly IConfigurationRoot _configuration;

    static DbConfig()
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "secure.env");
        if (File.Exists(envPath))
            DotNetEnv.Env.Load(envPath);

        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
    }

    public static IConfiguration Configuration => _configuration;
}
