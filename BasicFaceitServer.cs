using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using BasicFaceitServer.Config;

namespace BasicFaceitServer;

public class BasicFaceitServer : BasePlugin
{
    public override string ModuleName => "Basic Faceit Server";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Akbar Menglimuratov";
    public override string ModuleDescription => "Faceit server";

    private static ILogger<BasicFaceitServer> _logger;
    
    // public BasicFaceitServer Game { get; private set; } = new(_logger);
    
    public State States = new();

    private readonly Players _players = new();
    
    public MyConfigs Config { get; set; } = new();

    private CCSGameRules? _gameRules;
    
    
    private readonly Helper _helper;

    public BasicFaceitServer(ILogger<BasicFaceitServer> logger)
    {
        _logger = logger;
        _helper = new Helper(this, logger);
    }
    
    public override void Load(bool hotReload)
    {
        Logger.LogInformation("[Load] Start loading configs from config file");
        Config = ConfigManager.GetConfig(ModuleDirectory);
        
        ConfigManager.ValidateConfigs();
        _helper.SetTeamDataFromConfigs();

        // Listeners
        RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        
        // Game events
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventRoundAnnounceWarmup>(OnRoundAnnounceWarmup);
        RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
        RegisterEventHandler<EventRoundAnnounceMatchStart>(OnRoundAnnounceMatchStart);
        RegisterEventHandler<EventStartHalftime>(OnStartHalftime);
        RegisterEventHandler<EventSwitchTeam>(OnSwitchTeam);
        
        AddCommand("ct", "Switch team to CT", OnCTCommand);
        AddCommand("t", "Switch team to T", OnTCommand);
        AddCommand("clear_state", "Clear game states", OnClearStateCommand);
        AddCommand("print_state", "Print game slot", OnPrintStateCommand);
        AddCommand("clear_slot", "Clear players slot", OnClearSlotsCommand);
        AddCommand("print_slot", "Print players slot", OnPrintSlotsCommand);
        AddCommand("print_gr", "Print game rules", OnPrintGameRulesCommand);
        AddCommand("end_round", "End knife round", OnKnifeRoundEnd);
    }

    private void OnServerHibernationUpdate(bool isHibernating) {}

    private void OnMapEnd()
    {
        States = new State();
    }
    
    private void OnClientPutInServer(int playerSlot)
    {
        Logger.LogInformation($"[OnClientPutInServer]: Start");
        
        var player = _helper.GetPlayerBySlotId(playerSlot);
        if (player is null || !player.IsValid || player.IsBot) {
            Logger.LogInformation($"[OnClientPutInServer]: {player?.IpAddress} - Player is null or bot");
            return;
        }
        
        Logger.LogInformation($"""
            [OnClientPutInServer]: Player slot - {player.Slot}
            [OnClientPutInServer]: Player SteamID - {player.SteamID}
            [OnClientPutInServer]: Player IP - {player.IpAddress}
            [OnClientPutInServer]: Player connection status - {player.Connected.ToString()}
        """);
        
        if (player.IpAddress is null) return;

        if (!_helper.CheckIpInParticipantsList(player.IpAddress)) return;

        if (_players.Slots.TryGetValue(player.SteamID, out var slot)) {
            slot.ConnectionStatus = PlayerConnectedState.PlayerReconnecting;
            slot.Slot = player.Slot;
            return;
        }
         
        var pSlot = new Player
        {
            SteamId = player.SteamID,
            UserId = player.UserId,
            IpAddress = player.IpAddress,
            ConnectionStatus = PlayerConnectedState.PlayerConnecting,
            Slot = player.Slot
        };

        _players.Slots[player.SteamID] = pSlot;

        Logger.LogInformation($"[OnClientPutInServer]: End");
    }
    
    private void OnClientDisconnect(int playerSlot)
    {
        Logger.LogInformation($"[OnClientPutInServer]: Start");
        
        var player = _helper.GetPlayerBySlotId(playerSlot);
        if (player is null || !player.IsValid || player.IsBot) {
            Logger.LogInformation($"[OnClientDisconnect]: {player?.IpAddress} - Player is null or bot");
            return;
        }
        
        Logger.LogInformation($"""
            [OnClientDisconnect]: Player slot - {player.Slot}
            [OnClientDisconnect]: Player SteamID - {player.SteamID}
            [OnClientDisconnect]: Player IP - {player.IpAddress}
            [OnClientDisconnect]: Player connection status - {player.Connected.ToString()}
        """);

        if (_players.Slots.TryGetValue(player.SteamID, out var slot))
            slot.ConnectionStatus = PlayerConnectedState.PlayerDisconnecting;
        
        Logger.LogInformation($"[OnClientDisconnect]: End");
    }
    
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        Logger.LogInformation($"[{@event.EventName}]: Start");

        var player = @event.Userid;
        if (player is null || !player.IsValid || player.IsBot) {
            Logger.LogInformation($"[{@event.EventName}]: {player?.IpAddress} - Client is null or bot");
            return HookResult.Continue;
        }
        
        if (!_players.Slots.TryGetValue(player.SteamID, out var pSlot)) {
            player.ChangeTeam(CsTeam.Spectator);
            Logger.LogInformation($"[{@event.EventName}]: {player.IpAddress} - Client team {CsTeam.Spectator}");
            return HookResult.Handled;
        }

        if (pSlot.Team == CsTeam.None) {
            pSlot.Team = _helper.GetPlayerTeam(player);
        }
        
        Logger.LogInformation($"[{@event.EventName}]: {player.IpAddress} - Client team {pSlot.Team}");
        player.ChangeTeam(pSlot.Team);
        pSlot.ConnectionStatus = PlayerConnectedState.PlayerConnected;
        
        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return HookResult.Continue;

        if (_gameRules.GamePaused) {
            Server.PrintToChatAll("Ойынға 10 секундтан кейин старт бериледи");
            AddTimer(10.0f, () => {Server.ExecuteCommand("mp_unpause_match;");});
        }
        
        AddTimer(5.0f, () => {player.PrintToCenter("Пышақ роунды алдынан разминка");});
        
        if (States.MatchLive) return HookResult.Continue;
        if (States.PostKnifeWarmup) return HookResult.Continue;
        if (States.PreKnifeWarmup) return HookResult.Continue;
        if (States.KnifeRound)
        {
            _helper.RemovePlayerWeapons(player);
            return HookResult.Continue;
        }
        
        
        Logger.LogInformation($"[{@event.EventName}]: {player.IpAddress} - First player connected");
        
        States.PreKnifeWarmup = true;
        
        Server.ExecuteCommand($"mp_warmup_start; mp_warmuptime {Config.PreWarmupTime};");
        
        player.PrintToChat(_helper.GetColoredText("Пышақ роунды алдынан разминка!!!"));
        player.PrintToChat(_helper.GetColoredText("РАЗМИНКА!!!"));
        player.PrintToChat(_helper.GetColoredText("РАЗМИНКА!!!"));
        player.PrintToChat(_helper.GetColoredText("РАЗМИНКА!!!"));

        Logger.LogInformation($"[{@event.EventName}]: Finish");

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        Logger.LogInformation($"[{@event.EventName}]: Start");

        var player = @event.Userid;
        if (player is null || !player.IsValid || player.IsBot) {
            Logger.LogInformation($"[{@event.EventName}]: {player?.IpAddress} - Client is null or bot");
            return HookResult.Continue;
        }
        
        if (!_players.Slots.TryGetValue(player.SteamID, out var pSlot)) {
            return HookResult.Continue;
        }
        
        pSlot.ConnectionStatus = PlayerConnectedState.PlayerDisconnected;

        Logger.LogInformation($"[{@event.EventName}]: Finish");

        return HookResult.Continue;
    }
    
    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Logger.LogInformation($"[{@event.EventName}]: Start");

        if (States.MatchLive) return HookResult.Continue;

        var players = _helper.GetPlayers();

        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return HookResult.Continue;
        
        if (States.KnifeRound) {
            Logger.LogInformation($"[{@event.EventName}]: Knife round start. Skip intro and leave only knife");
            _gameRules.CTTeamIntroVariant = -1;
            _gameRules.TTeamIntroVariant = -1;
            
            foreach (var player in players)
                _helper.RemovePlayerWeapons(player);

            Server.PrintToChatAll(_helper.GetColoredText(("Пышақ роунды басланды")));
        } else if (States.MatchLive && players.Count < Config.MinPlayerToStart) {
            Logger.LogInformation($"[{@event.EventName}]: Players ({players.Count}) count is below 10. Pause the match");
            Server.ExecuteCommand("mp_pause_match;");
        }

        Logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Logger.LogInformation($"[{@event.EventName}]: Start");
        if (States.MatchLive) return HookResult.Continue;
        
        if (States.KnifeRound) {
            States.PostKnifeWarmup = true;
            States.KnifeRound = false;
            
            States.KnifeRoundWinner = @event.Winner == (byte) CsTeam.CounterTerrorist
                ? CsTeam.CounterTerrorist
                : CsTeam.Terrorist;
            
            Logger.LogInformation($"[{@event.EventName}]: Define knife round winner: {States.KnifeRoundWinner}");
            Logger.LogInformation($"[{@event.EventName}]: Start short warmup period");
            
            AddTimer(3.0f, () => { Server.ExecuteCommand($"mp_warmuptime {Config.PostWarmupTime}; mp_warmup_start;");});
        }

        Logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnRoundAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        if (!States.PostKnifeWarmup) return HookResult.Continue;
        
        Logger.LogInformation($"[{@event.EventName}]: Post knife warmup period started");

        var teamName1 = States.Teams.Team1.Name;
        var teamName2 = States.Teams.Team2.Name;
        var winnerTeamName = States.KnifeRoundWinner == CsTeam.CounterTerrorist
            ? teamName1
            : teamName2;
        Server.PrintToChatAll(_helper.GetColoredText($"{{green}}{winnerTeamName} {{white}}тəрепти таңлаң"));
        Server.PrintToChatAll(_helper.GetColoredText("!ct ямаса !t командасын жазың"));
            
        Logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
    {
        var players = _helper.GetPlayers();
        
        if (States.PreKnifeWarmup)
        {
            Logger.LogInformation($"[{@event.EventName}]: Pre knife warmup period ended");
            
            States.PreKnifeWarmup = false;
            States.KnifeRound = true;

            if (players.Count >= Config.MinPlayerToStart) return HookResult.Continue;
            
            Logger.LogInformation($"[{@event.EventName}]: Players ({players.Count}) count is below {Config.MinPlayerToStart}");
            Logger.LogInformation($"[{@event.EventName}]: Pause match before knife round");
            Server.ExecuteCommand($"mp_pause_match");
        }
        else if (States.PostKnifeWarmup)
        {
            Logger.LogInformation($"[{@event.EventName}]: Post knife warmup period ended");
            States.PostKnifeWarmup = false;
            States.MatchLive = true;
        }

        return HookResult.Continue;
    }
    
    private HookResult OnRoundAnnounceMatchStart(EventRoundAnnounceMatchStart  @event, GameEventInfo info)
    {
        Logger.LogInformation($"[{@event.EventName}]: Start");
        if (!States.MatchLive) return HookResult.Continue;
        
        Logger.LogInformation($"[{@event.EventName}]: Print Good luck message");
        Server.PrintToChatAll(_helper.GetColoredText("Ҳаммеге аўмет!!!"));
        
        Logger.LogInformation($"[{@event.EventName}]: End");
        return HookResult.Continue;
    }

    private HookResult OnStartHalftime(EventStartHalftime  @event, GameEventInfo info)
    {
        Server.ExecuteCommand($"mp_teamname_1 {States.Teams.Team2.Name}");
        Server.ExecuteCommand($"mp_teamname_2 {States.Teams.Team1.Name}");
        return HookResult.Continue;
    }

    private HookResult OnSwitchTeam(EventSwitchTeam  @event, GameEventInfo info)
    {
        Server.ExecuteCommand($"mp_teamname_1 {States.Teams.Team2.Name}");
        Server.ExecuteCommand($"mp_teamname_2 {States.Teams.Team1.Name}");
        return HookResult.Continue;
    }
    
    private void OnCTCommand(CCSPlayerController? player, CommandInfo command)
    {
        Logger.LogInformation($"On command execute: !ct - Start");
        if (States.MatchLive) return;
        
        if (player == null || !player.IsValid) return;

        if (player.Team != States.KnifeRoundWinner || player.Team == CsTeam.Spectator) return;

        Logger.LogInformation($"On command execute: !ct - Player team: {player.Team}");
        
        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return;
        _gameRules.SwapTeamsOnRestart = player.Team == CsTeam.Terrorist;

        _gameRules.WarmupPeriod = false;
        
        States.MatchLive = true;
        States.PostKnifeWarmup = false;
        
        Logger.LogInformation($"On command execute: !ct - Exec gamemode_competitive, restart game (1 sec)");
        Server.ExecuteCommand("exec gamemode_competitive; mp_restartgame 1;");

        Logger.LogInformation($"On command execute: !ct - End");
    }
    
    private void OnTCommand(CCSPlayerController? player, CommandInfo command)
    {
        Logger.LogInformation($"On command execute: !t - Start");
        if (States.MatchLive) return;
         
        if (player == null || !player.IsValid) return;

        if (player.Team != States.KnifeRoundWinner || player.Team == CsTeam.Spectator) return;

        Logger.LogInformation($"On command execute: !t - Player team: {player.Team}");

        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return;
        _gameRules.SwapTeamsOnRestart = player.Team == CsTeam.CounterTerrorist;

        _gameRules.WarmupPeriod = false;
        
        States.MatchLive = true;
        States.PostKnifeWarmup = false;

        Logger.LogInformation($"On command execute: !t - Exec gamemode_competitive, restart game (1 sec)");
        Server.ExecuteCommand("exec gamemode_competitive; mp_restartgame 1;");

        Logger.LogInformation($"On command execute: !t - End");
    }

    private void OnClearStateCommand(CCSPlayerController? player, CommandInfo command)
    {
        States = new State();
    }
    
    private void OnPrintStateCommand(CCSPlayerController? player, CommandInfo command)
    {
        Type type = typeof(State);
        
        // Get all public properties
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            // Print property name and value
            Console.WriteLine($"{property.Name} = {property.GetValue(States)}");
        }
    }

    private void OnClearSlotsCommand(CCSPlayerController? player, CommandInfo command)
    {
        var players = _players.Slots.ToList();
        
        foreach (var p in players)
        {
            Console.WriteLine($"{p.Key}: {p.Value}");
        }
        _players.Slots.Clear();
    }
    
    private void OnPrintSlotsCommand(CCSPlayerController? player, CommandInfo command)
    {
        var players = _players.Slots;
        
        foreach (var p in players)
        {
            Console.WriteLine($"{p.Key}: {p.Value}");
        }
    }
    
    private void OnPrintGameRulesCommand(CCSPlayerController? player, CommandInfo command)
    {    
        _helper.PrintGameRules();
    }

    private void OnKnifeRoundEnd(CCSPlayerController? player, CommandInfo command)
    {
        _gameRules = _helper.GetGameRules();
        var cmdArg = command.GetArg(1);
        var reason = cmdArg switch
        {
            "ct" => RoundEndReason.CTsWin,
            "t" => RoundEndReason.TerroristsWin,
            _ => RoundEndReason.Unknown
        };
        Console.WriteLine(cmdArg);
        _gameRules.TerminateRound(1.0f, reason);
    }
    
}
