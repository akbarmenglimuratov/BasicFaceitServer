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
            { "{purple}", 3 },
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
        logger.LogInformation("[Helper][GetPlayerTeam] - Get client team (CT, T or Spectator)");
        var configs = game.Config;
        var playerIp = player.IpAddress?.Split(":")[0];

        if (string.IsNullOrEmpty(playerIp))
            return CsTeam.Spectator;

        var cabin = configs.Cabins.FirstOrDefault(c => c.IsActive && c.IpAddresses.Contains(playerIp));
        if (cabin == null)
            return CsTeam.Spectator;

        var liveTeam = configs.LiveGame.FirstOrDefault(t => t.CabinId == cabin.Id);
        if (liveTeam == null)
            return CsTeam.Spectator;

        return liveTeam.DefaultTeam switch
        {
            "CT" => CsTeam.CounterTerrorist,
            "T" => CsTeam.Terrorist,
            _ => CsTeam.Spectator
        };
    }

    public void SetTeamDataFromConfigs()
    {
        logger.LogInformation("[Helper][SetTeamDataFromConfigs] - Set team data (names)");
        var configs = game.Config;

        var firstLive = configs.LiveGame.First(t => t.DefaultTeam == "CT");
        var secondLive = configs.LiveGame.First(t => t.DefaultTeam == "T");

        var team1 = configs.Teams.First(t => t.Id == firstLive.TeamId);
        var team2 = configs.Teams.First(t => t.Id == secondLive.TeamId);

        game.States.Teams.Team1 = new TeamData(team1.Id, team1.Name);
        game.States.Teams.Team2 = new TeamData(team2.Id, team2.Name);

        logger.LogInformation($"[Helper][SetTeamDataFromConfigs] - Team1 - {team1.Name}");
        logger.LogInformation($"[Helper][SetTeamDataFromConfigs] - Team2 - {team2.Name}");
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
        Server.PrintToChatAll($"TTeamIntroVariant: {gameRules.TTeamIntroVariant}");
        Server.PrintToChatAll($"CTTeamIntroVariant: {gameRules.CTTeamIntroVariant}");
        Server.PrintToChatAll($"PlayedTeamIntroVO: {gameRules.PlayedTeamIntroVO}");
        Server.PrintToChatAll($"TeamIntroPeriod: {gameRules.TeamIntroPeriod}");
        Server.PrintToChatAll($"TeamIntroPeriodEnd: {gameRules.TeamIntroPeriodEnd}");
        Server.PrintToChatAll($"MatchEndCount: {gameRules.MatchEndCount}");
        Server.PrintToChatAll($"RoundStartRoundNumber: {gameRules.RoundStartRoundNumber}");
        Server.PrintToChatAll($"RoundStartTime: {gameRules.RoundStartTime}");
        Server.PrintToChatAll($"ForceTeamChangeSilent: {gameRules.ForceTeamChangeSilent}");
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
        logger.LogInformation(
            "[Helper][CheckIpInParticipantsList] - Check if player in participants list - {ipAddress}",
            playerIpAddress);
        if (playerIpAddress == "") return false;

        var configs = game.Config;

        var playerIp = playerIpAddress.Split(":")[0];

        var isParticipant = configs.Cabins.Any(c =>
            configs.LiveGame.Any(l => c.Id == l.CabinId)
            && c.IpAddresses.Contains(playerIp)
        );
        logger.LogInformation($"[Helper][CheckIpInParticipantsList] - The participant - {isParticipant}");
        return isParticipant;
    }

    public void PlayerJoinTeam(CCSPlayerController player, CsTeam playerTeam)
    {
        logger.LogInformation($"[Helper][PlayerJoinTeam]: Player team - {playerTeam.ToString()}");

        game.AddTimer(0.1f, () =>
        {
            player.Respawn();
            game.AddTimer(0.1f, () => { player.ChangeTeam(playerTeam); });
        });
    }

    public void PreparePlayerForKnifeRound(CCSPlayerController player)
    {
        RemovePlayerWeapon(player);
        SetPlayerAccountToZero(player);
        GivePlayerArmor(player);
        GivePlayerKnife(player);
    }

    private void DropWeapon(CCSPlayerController player, string weaponName)
    {
        logger.LogInformation("Weapon design name: " + weaponName);
        var weaponServices = player.PlayerPawn?.Value?.WeaponServices;

        if (weaponServices == null)
            return;

        var matchedWeapon = weaponServices.MyWeapons
            .FirstOrDefault(w => w?.IsValid == true && w.Value != null && w.Value.DesignerName == weaponName);

        try
        {
            if (matchedWeapon?.IsValid != true) return;
            weaponServices.ActiveWeapon.Raw = matchedWeapon.Raw;

            var weaponEntity = weaponServices.ActiveWeapon.Value?.As<CBaseEntity>();
            if (weaponEntity == null || !weaponEntity.IsValid)
                return;

            player.DropActiveWeapon();
            Server.NextFrame(() => { weaponEntity.AddEntityIOEvent("Kill", weaponEntity, null, "", 0.1f); });
        }
        catch (Exception ex)
        {
            logger.LogError("Error while Refreshing Weapon via className: {ex}", ex.Message);
        }
    }

    private void RemovePlayerWeapon(CCSPlayerController player)
    {
        if (!player.IsValid || player.PlayerPawn.Value == null) return;

        DropWeapon(player, "weapon_c4");
        player.RemoveWeapons();
    }

    private void SetPlayerAccountToZero(CCSPlayerController player)
    {
        logger.LogInformation("[Helper][SetPlayerAccountToZero] - Set player money to zero");
        var playerMoney = player.InGameMoneyServices;
        if (playerMoney is null) return;

        playerMoney.Account = 0;
        Utilities.SetStateChanged(player, "CCSPlayerController_InGameMoneyServices", "m_iAccount");
    }

    private void GivePlayerArmor(CCSPlayerController player)
    {
        logger.LogInformation("[Helper][GivePlayerArmor] - Give player armor");
        player.GiveNamedItem("item_kevlar");
    }

    private void GivePlayerKnife(CCSPlayerController player)
    {
        logger.LogInformation("[Helper][GivePlayerKnife] - Give player knife");
        var knifeDesignName = player.Team == CsTeam.CounterTerrorist
            ? "weapon_knife"
            : "weapon_knife_t";

        player.GiveNamedItem(knifeDesignName);
    }

    public void SetTeamName(TeamData ctTeam, TeamData tTeam)
    {
        // var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
        // foreach (var team in teams)
        // {
        //     team.Teamname = (CsTeam) team.TeamNum switch
        //     {
        //         CsTeam.CounterTerrorist => ctTeam.Name,
        //         CsTeam.Terrorist => tTeam.Name,
        //         _ => team.Teamname
        //     };
        //     Utilities.SetStateChanged(team, "CTeam", "m_szTeamname");
        // }

        // TODO: it's not working correctly. Fix it later
    }
}