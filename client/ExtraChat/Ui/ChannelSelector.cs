using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace ExtraChat.Ui;

internal class ChannelSelector {
    private Plugin Plugin { get; }

    internal ChannelSelector(Plugin plugin) {
        this.Plugin = plugin;
    }

    internal void Draw() {
        if (!this.Plugin.PluginUi.ChannelSelectorVisible) return;
        if (!this.Plugin.ClientState.IsLoggedIn) return;

        var orderedChannels = this.Plugin.ConfigInfo.ChannelOrder
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value)
            .ToList();

        if (orderedChannels.Count == 0) return;

        ImGui.SetNextWindowSize(new Vector2(160, 0) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

        var visible = true;
        if (!ImGui.Begin("ExtraChat 频道", ref visible)) {
            ImGui.End();
            return;
        }

        if (!visible) {
            this.Plugin.PluginUi.ChannelSelectorVisible = false;
            this.Plugin.GameFunctions.ResetOverride();
            ImGui.End();
            return;
        }

        var currentOverride = this.Plugin.GameFunctions.OverrideChannel;

        if (currentOverride == Guid.Empty) {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.6f, 0.3f, 1f));
        }
        if (ImGui.Button("默认", new Vector2(-1, 0))) {
            this.Plugin.GameFunctions.ResetOverride();
        }
        if (currentOverride == Guid.Empty) {
            ImGui.PopStyleColor();
        }

        ImGui.Separator();

        foreach (var id in orderedChannels) {
            var name = this.Plugin.ConfigInfo.GetName(id);
            if (id == currentOverride) {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.6f, 0.3f, 1f));
            }
            if (ImGui.Button(name, new Vector2(-1, 0))) {
                this.Plugin.GameFunctions.OverrideChannel = id;
            }
            if (id == currentOverride) {
                ImGui.PopStyleColor();
            }
        }

        ImGui.End();
    }
}
