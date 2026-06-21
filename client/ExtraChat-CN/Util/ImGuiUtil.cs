using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace ExtraChat.Util;

internal static class ImGuiUtil {
    private static readonly Dictionary<int, string?[]> Tutorials = new() {
        [0] = new[] {
            "创建频道",

            "点击这个按钮可以创建一个新的 ExtraChat 频道。",
            "你也可以让好友邀请你加入他们的频道。",
        },
        [1] = new[] {
            "刷新数据",

            "点击这个按钮可以刷新所有频道数据。",
            "通常不需要手动刷新；点击某个频道时，成员列表会自动更新。",
        },
        [2] = new[] {
            "管理已加入的频道",

            "点击列表中的频道，右侧会显示该频道的成员。",
            "也可以右键点击频道名称，打开包含各项操作的菜单。",
            "如果你收到了频道邀请，可以在这个菜单里接受邀请。",
        },
        [3] = new[] {
            "在频道中发言",

            "频道名称前显示的数字就是频道编号。",
            "右键点击频道名称即可修改编号。",
            "这个编号决定你要使用哪个命令在对应频道发言。",
            "例如，编号为 1 的频道使用 /ecl1，编号为 2 的频道使用 /ecl2，依此类推。",
            null,
            "点击这个频道可以查看成员列表。",
        },
        [4] = new[] {
            "频道成员",

            "成员列表中，每个成员前面会显示其身份标记。",
            null,
            "[管理员] = 频道管理员",
            "[队长] = 频道的队长，可以踢人和邀请",
            "[组员] = 普通成员",
            "[待确认] = 待确认的受邀者",
            null,
            "离线成员会以灰色显示。",
        },
        [5] = new[] {
            "管理频道成员",

            "频道的队长和管理员可以右键点击成员列表中的成员，打开成员操作菜单。",
            "为了避免误操作，许多选项需要按住 Ctrl 键才可使用。",
        },
    };

    internal static bool IconButton(FontAwesomeIcon icon, string? id = null, string? tooltip = null) {
        var label = icon.ToIconString();
        if (id != null) {
            label += $"##{id}";
        }

        ImGui.PushFont(UiBuilder.IconFont);
        var ret = ImGui.Button(label);
        ImGui.PopFont();

        if (tooltip != null && ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tooltip);
            ImGui.EndTooltip();
        }

        return ret;
    }

    internal static bool SelectableConfirm(string label, ConfirmKey keys = ConfirmKey.Ctrl, string? tooltip = null) {
        var selectable = ImGui.Selectable(label);
        var hovered = ImGui.IsItemHovered();

        var confirmHeld = true;
        var mods = hovered ? new StringBuilder() : null;
        foreach (var key in Enum.GetValues<ConfirmKey>()) {
            if (!keys.HasFlag(key)) {
                continue;
            }

            if (hovered) {
                if (mods!.Length != 0) {
                    mods.Append('+');
                }

                mods.Append(key.ToString());
            }

            var held = key switch {
                ConfirmKey.Ctrl => ImGui.GetIO().KeyCtrl,
                ConfirmKey.Alt => ImGui.GetIO().KeyAlt,
                ConfirmKey.Shift => ImGui.GetIO().KeyShift,
                _ => false,
            };
            confirmHeld &= held;
        }

        if (!confirmHeld && hovered) {
            ImGui.BeginTooltip();
            var explainer = $"按住 {mods} 才能使用此选项。";
            var tip = tooltip == null ? explainer : $"{tooltip}\n{explainer}";
            ImGui.TextUnformatted(tip);
            ImGui.EndTooltip();
        }

        return selectable && confirmHeld;
    }

    internal static bool Tutorial(Plugin plugin, int step) {
        var save = false;
        ref var current = ref plugin.ConfigInfo.TutorialStep;
        if (current < 0 || current != step) {
            return save;
        }

        if (!Tutorials.TryGetValue(step, out var strings)) {
            return save;
        }

        var max = Tutorials.Keys.Max();

        const string popupId = "extrachat-tutorial";
        ImGui.OpenPopup(popupId);

        ImGui.GetForegroundDrawList().AddRect(
            ImGui.GetItemRectMin() - new Vector2(2) * ImGuiHelpers.GlobalScale,
            ImGui.GetItemRectMax() + new Vector2(2) * ImGuiHelpers.GlobalScale,
            ImGui.GetColorU32(new Vector4(1, 0, 0, 1))
        );

        ImGui.SetNextWindowPos(ImGui.GetItemRectMax() + new Vector2(2) * ImGuiHelpers.GlobalScale);
        ImGui.SetNextWindowSize(new Vector2(350, 0) * ImGuiHelpers.GlobalScale);
        if (!ImGui.BeginPopup(popupId, ImGuiWindowFlags.AlwaysAutoResize)) {
            return save;
        }

        ImGui.PushFont(UiBuilder.DefaultFont);
        ImGui.PushTextWrapPos();

        ImGui.TextUnformatted(strings[0]);
        ImGui.Separator();

        foreach (var body in strings[1..]) {
            if (body == null) {
                ImGui.Spacing();
                continue;
            }

            ImGui.TextUnformatted(body);
        }

        if (step == max) {
            if (ImGui.Button("完成")) {
                current = -1;
                save = true;
            }
        } else {
            if (ImGui.Button("下一步")) {
                current += 1;
                save = true;
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("跳过教程")) {
            current = -1;
            save = true;
        }

        ImGui.PopTextWrapPos();
        ImGui.PopFont();
        ImGui.EndPopup();

        return save;
    }
}

[Flags]
internal enum ConfirmKey {
    Ctrl = 1 << 0,
    Alt = 1 << 1,
    Shift = 1 << 2,
}
