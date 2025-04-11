using System.Text.Json;

namespace BasicFaceitServer.Config;

public static class ConfigManager
{
    private const string ConfigPath = "configs.json";

    private static MyConfigs Config { get; set; } = new();

    public static MyConfigs GetConfig(string moduleDirectory)
    {
        string cfgFullPath = Path.Combine(moduleDirectory, ConfigPath);
        try {
            if (!File.Exists(cfgFullPath)) {
                Console.WriteLine("[ConfigManager]: Config file does not exists, saving defaults.");
                
                Config = new MyConfigs();
                File.WriteAllText(cfgFullPath, JsonSerializer.Serialize(
                    Config,
                     new JsonSerializerOptions { WriteIndented = true }
                ));
                
                return new MyConfigs();
            }

            string json = File.ReadAllText(cfgFullPath);
            var tmpConfig = JsonSerializer.Deserialize<MyConfigs>(json);
            if (tmpConfig == null) {
                Console.WriteLine("[ConfigManager]: Failed to parse config, using defaults.");
                return new MyConfigs();
            }

            Console.WriteLine("[ConfigManager]: Processed the config file, now using it");
            return tmpConfig;
        } catch (Exception ex){
            Console.WriteLine("[ConfigManager]: Exception reading config: " + ex.Message);
            Console.WriteLine("[ConfigManager]: Using defaults...");
            return new MyConfigs();
        }
    }

    public static void ValidateConfigs()
    {
        if (Config.Cabins is null || Config.Cabins.Length == 0) {
            throw new Exception("[ConfigManager]: Cabins are null or empty");
        }
        Console.WriteLine("[ConfigManager]: Cabins are loaded");
        
        if (Config.LiveGames is null) {
            throw new Exception("[ConfigManager]: LiveGames are null or empty");
        }
        Console.WriteLine("[ConfigManager]: Live match is loaded");

        if (Config.Cabins.Any(cabin => cabin.IpAddresses.Length == 0)) {
            throw new Exception("[Server] Cabin[i] ip_addresses is null or empty");
        }
        Console.WriteLine("[ConfigManager]: Ip addresses are loaded");
    }
}