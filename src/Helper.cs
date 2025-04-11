using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace BasicFaceitServer;

public class Helper(BasicFaceitServer game, ILogger<BasicFaceitServer> logger)
{
    public string GetColoredText(string message)
    {
        Dictionary<string, int> colorMap = new()
        {
            { "{default}", 1 },
            { "{white}", 1 },
            { "{darkred}", 2 },
            { "{purple}", 3},
            { "{green}", 4 },
            { "{lightgreen}", 5 },
            { "{slimegreen}", 6 },
            { "{red}", 7 },
            { "{grey}", 8 },
            { "{yellow}", 9 },
            { "{invisible}", 10 },
            { "{lightblue}", 11 },
            { "{blue}", 12 },
            { "{lightpurple}", 13 },
            { "{pink}", 14 },
            { "{fadedred}", 15 },
            { "{gold}", 16 },
            // No more colors are mapped to CS2
        };

        message = $"[{{green}}KINGS{{white}}]: {message}";
        const string pattern = "{(\\w+)}";
        var replaced = Regex.Replace(message, pattern, match =>
        {
            var colorCode = match.Groups[1].Value;
            return colorMap.TryGetValue("{" + colorCode + "}", out var replacement)
                ? Convert.ToChar(replacement).ToString()
                : match.Value;
        });

        // Non-breaking space - a little hack to get all colors to show
        return $"\u200B{replaced}";
    }
    
    public List<CCSPlayerController> GetPlayers(CsTeam? includeTeam = null) 
    {
        logger.LogInformation("[Helper][GetPlayers] - Get players (CT, T)");
        var playerList = Utilities
            .FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller")
            .Where(player =>  
                player.IsValid
                && (player is { IsBot: false, IsHLTV: false })
                && (includeTeam == null || player.Team == includeTeam)
                && player.Team != CsTeam.None
                && player.Team != CsTeam.Spectator
            )
            .ToList();

        return playerList;
    }
    
    public CsTeam GetPlayerTeam(CCSPlayerController player)
    {
        logger.LogInformation("[Helper][GetPlayerTeam] - Get player team (CT, T or Spectator)");
        var configs = game.Config;
        var playerIp = player.IpAddress?.Split(":")[0];

        List<int> liveCabins = [configs.LiveGames.Team1.CabinId, configs.LiveGames.Team2.CabinId];

        foreach (var cabin in configs.Cabins)
        {
            if (!liveCabins.Contains(cabin.Id)) continue;

            if (cabin.IsActive && cabin.IpAddresses.Contains(playerIp))
            {
                return cabin.JoinTeam == "CT" ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
            }
        }

        return CsTeam.Spectator;
    }

    public void SetTeamDataFromConfigs()
    {
        logger.LogInformation("[Helper][SetTeamDataFromConfigs] - Set team data (names)");
        var configs = game.Config;

        var team1 = configs.Teams.First(t => t.Id == configs.LiveGames.Team1.Id);
        var team2 = configs.Teams.First(t => t.Id == configs.LiveGames.Team2.Id);
        
        game.States.Teams.Team1 = new TeamData(team1.Id, team1.Name);
        game.States.Teams.Team2 = new TeamData(team2.Id, team2.Name);
        Console.WriteLine($"Team 1: {team1.Name}");
        Console.WriteLine($"Team 2: {team2.Name}");
        Console.WriteLine($"OnConfigParsed: Team1 - {team1.Name} vs Team2 - {team2.Name}");
        Server.ExecuteCommand($"mp_teamname_1 {team1.Name}");
        Server.ExecuteCommand($"mp_teamname_2 {team2.Name}");
    }

    public CCSGameRules? GetGameRules()
    {
        var gameRules = Utilities
            .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .First()
            .GameRules;

        return gameRules ?? null;
    }

    public void PrintGameRules()
    {
        var gameRulesEntities = Utilities
            .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        
        var gameRules = gameRulesEntities.First().GameRules;
        if (gameRules is null) return;
        
        Server.PrintToChatAll($"WarmupPeriod: {gameRules.WarmupPeriod}");
        Server.PrintToChatAll($"WarmupPeriodEnd: {gameRules.WarmupPeriodEnd}");
        Server.PrintToChatAll($"GamePhase: {gameRules.GamePhase}");
        Server.PrintToChatAll($"GameStartTime: {gameRules.GameStartTime}");
        Server.PrintToChatAll($"EndMatchOnThink: {gameRules.EndMatchOnThink}");
        Server.PrintToChatAll($"EndMatchOnRoundReset: {gameRules.EndMatchOnRoundReset}");
        Server.PrintToChatAll($"CompleteReset: {gameRules.CompleteReset}");
        Server.PrintToChatAll($"GameRestart: {gameRules.GameRestart}");
        Server.PrintToChatAll($"HasMatchStarted: {gameRules.HasMatchStarted}");
        Server.PrintToChatAll($"SwitchingTeamsAtRoundReset: {gameRules.SwitchingTeamsAtRoundReset}");
        Server.PrintToChatAll($"TTeamIntroVariant: {gameRules.TTeamIntroVariant }");
        Server.PrintToChatAll($"PlayedTeamIntroVO: {gameRules.PlayedTeamIntroVO }");
        Server.PrintToChatAll($"TeamIntroPeriod: {gameRules.TeamIntroPeriod }");
        Server.PrintToChatAll($"TeamIntroPeriodEnd: {gameRules.TeamIntroPeriodEnd }");
        Server.PrintToChatAll($"MatchEndCount: {gameRules.MatchEndCount }");
        Server.PrintToChatAll($"RoundStartRoundNumber: {gameRules.RoundStartRoundNumber  }");
        Server.PrintToChatAll($"RoundStartTime: {gameRules.RoundStartTime  }");
        Server.PrintToChatAll($"ForceTeamChangeSilent: {gameRules.ForceTeamChangeSilent   }");
        Server.PrintToChatAll($"RoundEndTimerTime: {gameRules.RoundEndTimerTime}");
        Server.PrintToChatAll($"FreezeTime: {gameRules.FreezeTime}");
        Server.PrintToChatAll($"SwapTeamsOnRestart: {gameRules.SwapTeamsOnRestart}");
        Server.PrintToChatAll($"MatchEndCount: {gameRules.MatchEndCount}");
        Server.PrintToChatAll($"GamePaused: {gameRules.LastFreezeEndBeep}");
        Server.PrintToChatAll("------------------------------------------------------");
    }

    public CCSPlayerController? GetPlayerBySlotId(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        return player ?? null;
    }

    public bool CheckIpInParticipantsList(string playerIpAddress)
    {
        logger.LogInformation("[Helper][CheckIpInParticipantsList] - Check if player in participants list");
        if (playerIpAddress == "") return false;

        var configs = game.Config;
        
        var playerIp = playerIpAddress.Split(":")[0];

        return configs.Cabins.Any(c =>
            (c.Id == configs.LiveGames.Team1.CabinId || c.Id == configs.LiveGames.Team2.CabinId)
            && c.IpAddresses.Contains(playerIp)
        );
    }

    public void RemovePlayerWeapons(CCSPlayerController player)
    {
        logger.LogInformation("[Helper][RemovePlayerWeapons] - Remove player weapon and give only knife");
        player.RemoveWeapons();
        var giveNamedItem = player.Team == CsTeam.CounterTerrorist 
            ? player.GiveNamedItem("weapon_knife")
            : player.GiveNamedItem("weapon_knife_t");
        player.GiveNamedItem("item_kevlar");
            
        var playerMoney = player.InGameMoneyServices;
        if (playerMoney is null) return;
        
        playerMoney.Account = 0;
        Utilities.SetStateChanged(player, "CCSPlayerController_InGameMoneyServices", "m_iAccount");
    }
}
