using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace ExtraChat.Util;

internal static class WorldUtil {
    private static readonly Dictionary<ushort, string> WorldNames = new();
    private static IPlayerCharacter? _localPlayer;

    internal static void SetLocalPlayer(IPlayerCharacter? player) {
        _localPlayer = player;
    }

    internal static void Initialise(IDataManager data) {
        WorldNames.Clear();

        var worlds = data.GetExcelSheet<World>();
        if (worlds == null) {
            return;
        }

        foreach (var world in worlds) {
            WorldNames[(ushort) world.RowId] = world.Name.ExtractText();
            if (world.RowId > 1000) {
                Plugin.Log.Debug($"World {world.RowId}: {world.Name.ExtractText()} (public={world.IsPublic})");
            }
        }
    }

    internal static string WorldName(ushort id) {
        if (WorldNames.TryGetValue(id, out var name)) {
            return name;
        }

        if (_localPlayer?.HomeWorld.RowId == id) {
            return _localPlayer.HomeWorld.Value.Name.ExtractText();
        }

        return $"({id})";
    }
}
