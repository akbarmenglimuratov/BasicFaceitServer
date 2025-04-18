using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BasicFaceitServer;

public class ConfigManager(BasicFaceitServer game, ILogger<BasicFaceitServer> logger)
{
    private const string ConfigPath = "configs.json";

    private static MyConfigs Config { get; set; } = new();

    public MyConfigs GetConfig(string moduleDirectory)
    {
        var cfgFullPath = Path.Combine(moduleDirectory, ConfigPath);
        try
        {
            if (!File.Exists(cfgFullPath))
            {
                logger.LogInformation("[ConfigManager]: Config file does not exists, saving defaults.");

                Config = new MyConfigs();
                File.WriteAllText(cfgFullPath, JsonSerializer.Serialize(
                    Config,
                    new JsonSerializerOptions { WriteIndented = true }
                ));

                return new MyConfigs();
            }

            var json = File.ReadAllText(cfgFullPath);
            var tmpConfig = JsonSerializer.Deserialize<MyConfigs>(json);
            if (tmpConfig == null)
            {
                logger.LogInformation("[ConfigManager]: Failed to parse config, using defaults.");
                return new MyConfigs();
            }

            logger.LogInformation("[ConfigManager]: Processed the config file, now using it");
            return tmpConfig;
        }
        catch (Exception ex)
        {
            logger.LogInformation($"[ConfigManager]: Exception reading config: {ex.Message}");
            logger.LogInformation("[ConfigManager]: Using defaults...");
            return new MyConfigs();
        }
    }

    public void ValidateConfigs()
    {
        if (game.Config.Cabins is null || Config.Cabins.Length == 0)
            throw new Exception("[ConfigManager]: Cabins are null or empty");
        logger.LogInformation("[ConfigManager]: Cabins are loaded");

        if (game.Config.Cabins.Any(cabin => cabin.IpAddresses.Length == 0))
            throw new Exception("[ConfigManager]: Cabin[i] ip_addresses is null or empty");
        logger.LogInformation("[ConfigManager]: Ip addresses are loaded");

        if (game.Config.LiveGame is not { Length: 2 })
            throw new Exception("[ConfigManager]: Live game must have exactly two teams.");
        logger.LogInformation("[ConfigManager]: Live match is loaded");

        logger.LogInformation("[ConfigManager]: Config validation passed");
    }
}