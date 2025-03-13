using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CursePlayers;

public class CursePlayers : BasePlugin
{
    public override string ModuleName => "CursePlayers";
    public override string ModuleDescription => "https://steamcommunity.com/id/kenoxyd";
    public override string ModuleAuthor => "kenoxyd";
    public override string ModuleVersion => "1.0.0";

    private HashSet<string> cursedPlayers = new();

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    [CommandHelper(minArgs: 1, usage: "<playername>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [ConsoleCommand("css_curse")]
    public void CurseCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || !caller.IsValid) return;

        if (cursedPlayers.Contains(caller.PlayerName))
        {
            caller.PrintToChat("You have already used your curse this round.");
            return;
        }

        var target = FindPlayer(command.ArgByIndex(1), caller);
        if (target == null || !target.IsValid || target.Connected != PlayerConnectedState.PlayerConnected)
        {
            caller.PrintToChat("Player not found.");
            return;
        }

        if (target.Team == CsTeam.Spectator)
        {
            caller.PrintToChat("You can't curse someone in spectator mode. Pay more attention, silly goose!");
            return;
        }

        if (!target.PawnIsAlive)
        {
            caller.PrintToChat("You can't curse someone that's dead. Nice try!");
            return;
        }

        target.PrintToChat($"You have been cursed by {caller.PlayerName}!");
        var pawn = target.PlayerPawn.Value;

        pawn.Health -= 1;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        cursedPlayers.Add(caller.PlayerName);
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        cursedPlayers.Clear();
        return HookResult.Continue;
    }

    private static CCSPlayerController? FindPlayer(string identifier, CCSPlayerController? caller)
    {
        if (identifier == "@me" && caller != null)
        {
            return caller;
        }

        var matchingPlayers = Utilities.GetPlayers()
            .Where(p => p.PlayerName.Contains(identifier, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingPlayers.Count == 1)
        {
            return matchingPlayers.First();
        }

        if (matchingPlayers.Count > 1)
        {
            caller?.PrintToChat("Multiple players found with that name. Please refine your search.");
        }
        else
        {
            caller?.PrintToChat("No player found with that name.");
        }

        return null;
    }
}
