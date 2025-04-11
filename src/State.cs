using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace BasicFaceitServer;

public class TeamData(int id, string name)
{
    public int Id { get; set; } = id;
    public string Name { get; set; } = name;
}

public class Teams
{
    public TeamData Team1 { get; set; } = new TeamData(1, "CounterTerrorist");
    public TeamData Team2 { get; set; } = new TeamData(2, "Terrorist");
} 

public class State
{
    public bool PreKnifeWarmup { get; set; }

    public bool PostKnifeWarmup { get; set; }

    public bool KnifeRound { get; set; }

    public bool MatchLive { get; set; }
    
    public CsTeam KnifeRoundWinner { get; set; } = CsTeam.None;

    public CCSPlayerController? KnifeWinnerLeader { get; set; } = null;

    public Teams Teams { get; set; } = new();
}

public class Player
{
    public ulong SteamId { get; set; }
    
    public string? IpAddress { get; set; }

    public int Slot { get; set; }

    public PlayerConnectedState ConnectionStatus { get; set; } = PlayerConnectedState.PlayerNeverConnected;
    
    public CsTeam Team { get; set; } = CsTeam.None;

    public int? UserId { get; set; }
}

public class Players
{
    public Dictionary<ulong, Player> Slots { get; set; } = [];
}