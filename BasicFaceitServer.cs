using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace BasicFaceitServer;

public class BasicFaceitServer : BasePlugin
{
    public override string ModuleName => "Basic Faceit Server";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Akbar Menglimuratov";
    public override string ModuleDescription => "Faceit server";

    private readonly ILogger<BasicFaceitServer> _logger;

    public State States = new();

    public MyConfigs Config { get; private set; } = new();

    private CCSGameRules? _gameRules;

    private readonly Helper _helper;

    private readonly ConfigManager _configManager;

    public BasicFaceitServer(ILogger<BasicFaceitServer> l)
    {
        _logger = l;
        _helper = new Helper(this, l);
        _configManager = new ConfigManager(this, l);
    }

    public override void Load(bool hotReload)
    {
        _logger.LogInformation("[Load] Start loading configs from config file");
        Config = _configManager.GetConfig(ModuleDirectory);

        _configManager.ValidateConfigs();
        _helper.SetTeamDataFromConfigs();

        // Listeners
        RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

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
        RegisterEventHandler<EventBombPlanted>(OnEventBombPlanted);
        RegisterEventHandler<EventTeamIntroStart>(OnEventTeamIntroStart, HookMode.Pre);


        AddCommand("ct", "Switch team to CT", OnCTCommand);
        AddCommand("t", "Switch team to T", OnTCommand);
        AddCommand("print_state", "Print game slot", OnPrintStateCommand);
        AddCommand("print_gr", "Print game rules", OnPrintGameRulesCommand);
        AddCommand("end_round", "End knife round", OnKnifeRoundEnd);
    }

    private void OnServerHibernationUpdate(bool isHibernating)
    {
        if (!isHibernating) return;

        _logger.LogInformation($"[OnServerHibernationUpdate]: Hibernating. Reset game state");
        States = new State();
    }

    private void OnMapEnd()
    {
        States = new State();
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        _logger.LogInformation($"[{@event.EventName}]: Start");

        var player = @event.Userid;
        if (player is null || !player.IsValid || player.IsBot || player.IpAddress is null)
        {
            _logger.LogInformation($"[{@event.EventName}]: {player?.IpAddress} - Client is null or bot");
            return HookResult.Continue;
        }

        if (!_helper.CheckIpInParticipantsList(player.IpAddress))
        {
            _logger.LogInformation($"[{@event.EventName}]: Player is not participant - {player.IpAddress}");
            _logger.LogInformation($"[{@event.EventName}]: Player team {CsTeam.Spectator}");
            _helper.PlayerJoinTeam(player, CsTeam.Spectator);
            return HookResult.Handled;
        }

        if (player.Team is CsTeam.Spectator or CsTeam.None)
        {
            _logger.LogInformation($"[{@event.EventName}]: Player connecting first time. Assign team");
            var playerTeam = _helper.GetPlayerTeam(player);
            _helper.PlayerJoinTeam(player, playerTeam);
        }

        if (States.MatchLive || States.PostKnifeWarmup)
            return HookResult.Continue;

        if (States.KnifeRound)
        {
            _helper.PreparePlayerForKnifeRound(player);
            return HookResult.Handled;
        }

        var allPlayers = Utilities.GetPlayers();
        if (!States.PreKnifeWarmup && allPlayers.Count == 1)
        {
            States.PreKnifeWarmup = true;
            _logger.LogInformation($"[{@event.EventName}]: First player connected - {player.IpAddress}");
            Server.ExecuteCommand($"mp_warmup_start; mp_warmuptime {Config.PreWarmupTime};");
        }

        if (States.PreKnifeWarmup)
        {
            player.PrintToChat(_helper.GetColoredText("Пышақ роунды алдынан разминка!!!"));
            player.PrintToChat(_helper.GetColoredText("РАЗМИНКА!!!"));
            player.PrintToChat(_helper.GetColoredText("РАЗМИНКА!!!"));
            player.PrintToChat(_helper.GetColoredText("РАЗМИНКА!!!"));
            AddTimer(5.0f, () => player.PrintToCenter("Пышақ роунды алдынан разминка"));
            return HookResult.Continue;
        }

        _logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        _logger.LogInformation($"[{@event.EventName}]: Start");

        var player = @event.Userid;
        if (player is null || !player.IsValid || player.IsBot)
        {
            _logger.LogInformation($"[{@event.EventName}]: Player is null or bot - {player?.IpAddress}");
            return HookResult.Continue;
        }

        _logger.LogInformation($"[{@event.EventName}]: Player disconnected- {player.IpAddress}");
        _logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _logger.LogInformation($"[{@event.EventName}]: Start");

        if (States.MatchLive) return HookResult.Continue;

        var players = _helper.GetPlayers();

        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return HookResult.Continue;

        if (States.KnifeRound)
        {
            _logger.LogInformation($"[{@event.EventName}]: Knife round started. Skip team intro");
            // _gameRules.CTTeamIntroVariant = -1;
            // _gameRules.TTeamIntroVariant = -1;
            _gameRules.TeamIntroPeriod = false;

            foreach (var player in players)
                _helper.PreparePlayerForKnifeRound(player);

            Server.PrintToChatAll(_helper.GetColoredText(("Пышақ роунды басланды")));
        }
        else if (States.MatchLive && players.Count >= Config.MinPlayerToStart)
        {
            _logger.LogInformation(
                $"[{@event.EventName}]: Players ({players.Count}) count is below 10. Pause the match");
            Server.ExecuteCommand("mp_pause_match;");
        }

        _logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        _logger.LogInformation($"[{@event.EventName}]: Start");
        if (States.MatchLive) return HookResult.Continue;

        if (States.KnifeRound)
        {
            States.PostKnifeWarmup = true;
            States.KnifeRound = false;

            States.KnifeRoundWinner = @event.Winner == (byte)CsTeam.CounterTerrorist
                ? CsTeam.CounterTerrorist
                : CsTeam.Terrorist;

            _logger.LogInformation($"[{@event.EventName}]: Define knife round winner: {States.KnifeRoundWinner}");
            _logger.LogInformation($"[{@event.EventName}]: Start short warmup period");

            AddTimer(3.0f,
                () => { Server.ExecuteCommand($"mp_warmuptime {Config.PostWarmupTime}; mp_warmup_start;"); });
        }

        _logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnRoundAnnounceWarmup(EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        if (!States.PostKnifeWarmup) return HookResult.Continue;

        _logger.LogInformation($"[{@event.EventName}]: Post knife warmup period started");

        var teamName1 = States.Teams.Team1.Name;
        var teamName2 = States.Teams.Team2.Name;
        var winnerTeamName = States.KnifeRoundWinner == CsTeam.CounterTerrorist
            ? teamName1
            : teamName2;

        _logger.LogInformation($"[{@event.EventName}]: Team name 1 - {teamName1}");
        _logger.LogInformation($"[{@event.EventName}]: Team name 2 - {teamName2}");
        _logger.LogInformation($"[{@event.EventName}]: Winner team name - {winnerTeamName}");

        Server.PrintToChatAll(_helper.GetColoredText($"{{green}}{winnerTeamName} {{white}}тəрепти таңлаң"));
        Server.PrintToChatAll(_helper.GetColoredText("!ct ямаса !t командасын жазың"));

        _logger.LogInformation($"[{@event.EventName}]: Finish");
        return HookResult.Continue;
    }

    private HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
    {
        var players = _helper.GetPlayers();

        if (States.PreKnifeWarmup)
        {
            _logger.LogInformation($"[{@event.EventName}]: Pre-knife warmup period ended");

            States.PreKnifeWarmup = false;
            States.KnifeRound = true;

            if (players.Count < Config.MinPlayerToStart)
            {
                _logger.LogInformation(
                    $"[{@event.EventName}]: Players ({players.Count}) count is below {Config.MinPlayerToStart}");
                _logger.LogInformation($"[{@event.EventName}]: Pausing match before knife round");
                Server.ExecuteCommand("mp_pause_match");
            }
            
            return HookResult.Handled;
        }

        if (!States.PostKnifeWarmup) return HookResult.Continue;

        _logger.LogInformation($"[{@event.EventName}]: Post knife warmup period ended");
        States.PostKnifeWarmup = false;
        States.MatchLive = true;

        return HookResult.Continue;
    }

    private HookResult OnRoundAnnounceMatchStart(EventRoundAnnounceMatchStart @event, GameEventInfo info)
    {
        _logger.LogInformation($"[{@event.EventName}]: Start");
        if (!States.MatchLive) return HookResult.Continue;

        _logger.LogInformation($"[{@event.EventName}]: Print Good luck message");
        Server.PrintToChatAll(_helper.GetColoredText("Ҳаммеге аўмет!!!"));

        _logger.LogInformation($"[{@event.EventName}]: End");
        return HookResult.Continue;
    }

    private HookResult OnStartHalftime(EventStartHalftime @event, GameEventInfo info)
    {
        Server.ExecuteCommand($"mp_teamname_1 {States.Teams.Team2.Name}");
        Server.ExecuteCommand($"mp_teamname_2 {States.Teams.Team1.Name}");
        return HookResult.Continue;
    }

    private HookResult OnSwitchTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return HookResult.Continue;

        Server.ExecuteCommand($"mp_teamname_1 {States.Teams.Team2.Name}");
        Server.ExecuteCommand($"mp_teamname_2 {States.Teams.Team1.Name}");
        return HookResult.Continue;
    }

    private HookResult OnEventBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        _logger.LogInformation($"[{@event.EventName}]: Start");

        info.DontBroadcast = true;

        var players = _helper.GetPlayers();
        foreach (var player in players)
        {
            Server.NextFrame(() => { player.PrintToCenterAlert("Бомба койылды. Жарылыўына 40 секунд бар"); });
        }

        return HookResult.Continue;
    }

    private HookResult OnEventTeamIntroStart(EventTeamIntroStart @event, GameEventInfo info)
    {
        info.DontBroadcast = true;
        return States.KnifeRound ? HookResult.Handled : HookResult.Continue;
    }

    private void OnCTCommand(CCSPlayerController? player, CommandInfo command)
    {
        _logger.LogInformation($"On command execute: !ct - Start");
        if (States.MatchLive) return;

        if (player == null || !player.IsValid) return;

        if (player.Team != States.KnifeRoundWinner || player.Team == CsTeam.Spectator) return;

        _logger.LogInformation($"On command execute: !ct - Player team: {player.Team}");

        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return;
        _gameRules.SwapTeamsOnRestart = player.Team == CsTeam.Terrorist;

        _gameRules.WarmupPeriod = false;

        States.MatchLive = true;
        States.PostKnifeWarmup = false;

        _logger.LogInformation($"On command execute: !ct - Exec gamemode_competitive, restart game (1 sec)");
        Server.ExecuteCommand("exec gamemode_competitive; mp_restartgame 1;");

        _logger.LogInformation($"On command execute: !ct - End");
    }

    private void OnTCommand(CCSPlayerController? player, CommandInfo command)
    {
        _logger.LogInformation($"On command execute: !t - Start");
        if (States.MatchLive) return;

        if (player == null || !player.IsValid) return;

        if (player.Team != States.KnifeRoundWinner || player.Team == CsTeam.Spectator) return;

        _logger.LogInformation($"On command execute: !t - Player team: {player.Team}");

        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return;
        _gameRules.SwapTeamsOnRestart = player.Team == CsTeam.CounterTerrorist;

        _gameRules.WarmupPeriod = false;

        States.MatchLive = true;
        States.PostKnifeWarmup = false;

        _logger.LogInformation($"On command execute: !t - Exec gamemode_competitive, restart game (1 sec)");
        Server.ExecuteCommand("exec gamemode_competitive; mp_restartgame 1;");

        _logger.LogInformation($"On command execute: !t - End");
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

    private void OnPrintGameRulesCommand(CCSPlayerController? player, CommandInfo command)
    {
        _helper.PrintGameRules();
    }

    private void OnKnifeRoundEnd(CCSPlayerController? player, CommandInfo command)
    {
        _gameRules = _helper.GetGameRules();
        if (_gameRules is null) return;

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