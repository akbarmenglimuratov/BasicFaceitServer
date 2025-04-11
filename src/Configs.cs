using System.Net;
using System.Text.Json.Serialization;

namespace BasicFaceitServer;

public class TournamentData
{
    [JsonPropertyName("id")] public int Id { get; set; } = 3;
    [JsonPropertyName("name")] public string Name { get; set; } = "Kings Championship";
    [JsonPropertyName("dateFrom")] public string TournamentDateFrom { get; set; } = "15-04-2025 10:00";
    [JsonPropertyName("dateTo")] public string TournamentDateTo { get; set; } ="16-04-2025 17:00";

}

public class Team(int id, string name)
{
    [JsonPropertyName("id")] public int Id { get; set; } = id;
    [JsonPropertyName("name")] public string Name { get; set; } = name;
}

public class Cabin(int id, string name, bool isActive, string[] ipAddresses)
{
    [JsonPropertyName("id")] public int Id { get; set; } = id;
    [JsonPropertyName("name")] public string Name { get; set; } = name;
    [JsonPropertyName("is_active")] public bool IsActive { get; set; } = isActive;
    [JsonPropertyName("ip_addresses")] public string[] IpAddresses { get; set; } = ipAddresses;
}

public class LiveTeam(int id, int cabinId, string defaultTeam)
{
    [JsonPropertyName("id")] public int Id { get; set; } = id;
    [JsonPropertyName("cabin_id")] public int CabinId { get; set; } = cabinId;
    [JsonPropertyName("default_team")] public string DefaultTeam { get; set; } = defaultTeam;
}

public class LiveGameTeams
{
    [JsonPropertyName("team_1")] public LiveTeam Team1 { get; set; } = new(1, 1, "CT");
    [JsonPropertyName("team_2")] public LiveTeam Team2 { get; set; } = new(2, 2, "T");
}

public class MyConfigs
{
    [JsonPropertyName("tournament")] public TournamentData Tournament { get; set; } = new();
    [JsonPropertyName("teams")] public Team[] Teams { get; set; } = [
        new(1, "TeamLiquid"),
        new(2, "NaVi")
    ];
    [JsonPropertyName("cabins")] public Cabin[] Cabins { get; set; } = [
        new Cabin(1, "VIP1", true, [IPAddress.Any.ToString(), IPAddress.Any.ToString()])
    ];
    [JsonPropertyName("live_games")] public LiveGameTeams LiveGames { get; set; } = new();
    [JsonPropertyName("pre_warmup_time")] public int PreWarmupTime { get; set; } = 420;
    [JsonPropertyName("post_warmup_time")] public int PostWarmupTime { get; set; } = 60;
    [JsonPropertyName("min_player_to_start")] public int MinPlayerToStart { get; set; } = 10;
}