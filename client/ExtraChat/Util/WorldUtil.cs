using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace ExtraChat.Util;

internal static class WorldUtil {
    private static readonly HashSet<string> CnDataCenterNames = new() {
        "陆行鸟",
        "莫古力",
        "猫小胖",
        "豆豆柴",
    };

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
            var worldName = world.Name.ExtractText();
            if (world.DataCenter.ValueNullable?.Name.ExtractText() is not { } ||
                worldName.Contains("s-") ||
                CnDataCenterNames.Contains(worldName))
            {
                continue;
            }

            WorldNames[(ushort) world.RowId] = worldName;
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
