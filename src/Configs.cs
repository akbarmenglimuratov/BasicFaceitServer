using System.Net;
using System.Text.Json.Serialization;

namespace BasicFaceitServer.Config;

public class TournamentData
{
    [JsonPropertyName("id")] public int Id { get; set; } = 3;
    [JsonPropertyName("name")] public string Name { get; set; } = "Kings Championship";
    [JsonPropertyName("dateFrom")] public string TournamentDateFrom { get; set; } = "15-04-2025 10:00";
    [JsonPropertyName("dateTo")] public string TournamentDateTo { get; set; } ="16-04-2025 17:00";

}

public class Team
{
    [JsonPropertyName("id")] public int Id { get; set; } = 1;
    [JsonPropertyName("name")] public string Name { get; set; } = "Any";

    public Team(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

public class Cabin
{
    [JsonPropertyName("id")] public int Id { get; set; } = 1;
    [JsonPropertyName("name")] public string Name { get; set; } = "VIP";
    [JsonPropertyName("is_active")] public bool IsActive { get; set; } = true;
    [JsonPropertyName("join_team")] public string JoinTeam { get; set; } = "CT";
    [JsonPropertyName("ip_addresses")] public string[] IpAddresses { get; set; } = [
        IPAddress.Any.ToString(),
        IPAddress.Any.ToString()
    ];
}

public class LiveTeam
{
    [JsonPropertyName("id")] public int Id { get; set; } = 1;
    [JsonPropertyName("cabin_id")] public int CabinId { get; set; } = 1;

    public LiveTeam(int id, int cabinId)
    {
        Id = id;
        CabinId = cabinId;
    }
}

public class LiveGameTeams
{
    [JsonPropertyName("team_1")] public LiveTeam Team1 { get; set; } = new(1, 1);
    [JsonPropertyName("team_2")] public LiveTeam Team2 { get; set; } = new(2, 2);
}

public class MyConfigs
{
    [JsonPropertyName("tournament")] public TournamentData Tournament { get; set; } = new();
    [JsonPropertyName("teams")] public Team[] Teams { get; set; } = [
        new(1, "TeamLiquid"),
        new(2, "NaVi")
    ];
    [JsonPropertyName("cabins")] public Cabin[] Cabins { get; set; } = [
        new()
    ];
    [JsonPropertyName("live_games")] public LiveGameTeams LiveGames { get; set; } = new();
    [JsonPropertyName("pre_warmup_time")] public int PreWarmupTime { get; set; } = 420;
    [JsonPropertyName("post_warmup_time")] public int PostWarmupTime { get; set; } = 60;
    [JsonPropertyName("min_player_to_start")] public int MinPlayerToStart { get; set; } = 10;
}