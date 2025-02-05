using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

[RequireModule(typeof(CommandHandler))]
[Module("Show a message of the day to players who join the server", "1.2.0")]
public class MOTD : BattleBitModule
{
    public MOTDConfiguration Configuration { get; set; } = null!;

    [ModuleReference]
    public CommandHandler CommandHandler { get; set; } = null!;

    [ModuleReference]
    public dynamic? PlaceholderLib { get; set; }

    [ModuleReference]
    public dynamic? GranularPermissions { get; set; }

    private List<ulong> greetedPlayers = new();

    public override void OnModulesLoaded()
    {
        if (this.PlaceholderLib is null)
        {
            this.Logger.Info("PlaceholderLib not found. MOTD will only support basic numbered placeholders.");
        }

        this.CommandHandler.Register(this);
    }

    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.EndingGame)
        {
            greetedPlayers.Clear();
            greetedPlayers.AddRange(this.Server.AllPlayers.Select(p => p.SteamID));
        }

        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        if (this.greetedPlayers.Contains(player.SteamID))
        {
            return Task.CompletedTask;
        }

        if (this.GranularPermissions is not null && !this.GranularPermissions.HasPermission(player.SteamID, "MOTD.View"))
        {
            this.Logger.Debug($"Player {player.Name} ({player.SteamID}) does not have permission to view the MOTD.");
            return Task.CompletedTask;
        }

        this.ShowMOTD(new Context(new ChatSource(player), string.Empty, "motd", Array.Empty<string>(), Array.Empty<object?>(), this, this.CommandHandler, null));

        return Task.CompletedTask;
    }

    [CommandCallback("setmotd", Description = "Sets the MOTD", Permissions = new[] { "MOTD.Set" }, ConsoleCommand = true)]
    public void SetMOTD(Context context, string motd)
    {
        this.Configuration.MOTD = motd;
        this.Configuration.Save();
        this.ShowMOTD(context);
    }

    [CommandCallback("motd", Description = "Shows the MOTD", Permissions = new[] { "MOTD.View" })]
    public string ShowMOTD(Context context)
    {
        ChatSource? chatSource = context.Source as ChatSource;

        string message;
        if (this.PlaceholderLib is not null)
        {
            message = this.PlaceholderLib.Create(this.Configuration.MOTD)
            .AddParam("servername", this.Server.ServerName)
            .AddParam("gamemode", this.Server.Gamemode)
            .AddParam("map", this.Server.Map)
            .AddParam("daynight", this.Server.DayNight)
            .AddParam("mapsize", this.Server.MapSize.ToString().Trim('_'))
            .AddParam("currentplayers", this.Server.CurrentPlayerCount)
            .AddParam("inqueueplayers", this.Server.InQueuePlayerCount)
            .AddParam("maxplayers", this.Server.MaxPlayerCount)
            .AddParam("name", chatSource?.Invoker.Name ?? context.Source.GetType().Name)
            .AddParam("ping", chatSource?.Invoker.PingMs ?? 0).Build();
        }
        else
        {
            message = string.Format(this.Configuration.MOTD, chatSource?.Invoker.Name ?? context.Source.GetType().Name, chatSource?.Invoker.PingMs ?? 0, this.Server.ServerName, this.Server.Gamemode, this.Server.Map, this.Server.DayNight, this.Server.MapSize.ToString().Trim('_'), this.Server.CurrentPlayerCount, this.Server.InQueuePlayerCount, this.Server.MaxPlayerCount);
        }

        return message;
    }
}

public class MOTDConfiguration : ModuleConfiguration
{
    public string MOTD { get; set; } = "Welcome!";
}
