using System.Diagnostics;
using System.Numerics;
using System.Threading.Channels;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using ExtraChat.Protocol.Channels;
using ExtraChat.Util;
using Lumina.Excel.Sheets;
using Channel = System.Threading.Channels.Channel;

namespace ExtraChat.Ui;

internal class PluginUi : IDisposable {
    internal const string CrossWorld = "\ue05d";

    private Plugin Plugin { get; }
    private ChannelList ChannelList { get; }

    internal bool Visible;

    private readonly List<(uint Id, Vector4 Abgr)> _uiColours;

    internal PluginUi(Plugin plugin) {
        this.Plugin = plugin;
        this.ChannelList = new ChannelList(this.Plugin);

        this._uiColours = this.Plugin.DataManager.GetExcelSheet<UIColor>()!
            .Where(row => row.Dark is not (0 or 0x000000FF))
            .Select(row => (row.RowId, row.Dark, ColourUtil.Step(row.Dark)))
            .GroupBy(row => row.Dark)
            .Select(grouping => grouping.First())
            .OrderBy(row => row.Item3.Item1)
            .ThenBy(row => row.Item3.Item2)
            .ThenBy(row => row.Item3.Item3)
            .Select(row => (row.RowId, ImGui.ColorConvertU32ToFloat4(ColourUtil.RgbaToAbgr(row.Dark))))
            .ToList();

        this.Plugin.Interface.UiBuilder.Draw += this.Draw;
        this.Plugin.Interface.UiBuilder.OpenConfigUi += this.OpenConfigUi;

        if (this.Plugin.Interface.Reason == PluginLoadReason.Installer && this.Plugin.ConfigInfo.Key == null) {
            this.Visible = true;
        }
    }

    public void Dispose() {
        this.Plugin.Interface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
        this.Plugin.Interface.UiBuilder.Draw -= this.Draw;
    }

    private void OpenConfigUi() {
        this.Visible ^= true;
    }

    internal (string, ushort)? InviteInfo;

    internal volatile bool Busy;
    private string? _challenge;
    private Guid? _inviteId;
    private readonly Channel<string?> _challengeChannel = Channel.CreateUnbounded<string?>();

    private void Draw() {
        if (this._challengeChannel.Reader.TryRead(out var challenge)) {
            this._challenge = challenge;
        }

        this.DrawConfigWindow();
        this.DrawInviteWindow();
    }

    private void DrawConfigWindow() {
        if (!this.Visible) {
            return;
        }

        ImGui.SetNextWindowSize(new Vector2(500, 325) * ImGuiHelpers.GlobalScale, ImGuiCond.FirstUseEver);

        if (!ImGui.Begin(Plugin.Name, ref this.Visible)) {
            ImGui.End();
            return;
        }

        if (!this.Plugin.ClientState.IsLoggedIn) {
            ImGui.TextUnformatted("请先登录角色。");
            ImGui.End();
            return;
        }

        if (ImGui.BeginTabBar("tabs")) {
            if (ImGui.BeginTabItem("频道")) {
                var status = this.Plugin.Client.Status;
                ImGui.TextUnformatted($"状态: {status}");
                ImGui.SameLine();
                if (ImGuiUtil.IconButton(FontAwesomeIcon.Wifi, tooltip: "重新连接") && !this.Busy) {
                    this.Busy = true;

                    Task.Run(async () => {
                        this.Plugin.Client.StopLoop();
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        this.Plugin.Client.StartLoop();
                        this.Busy = false;
                    });
                }

                switch (status) {
                    case Client.State.Connected:
                        this.ChannelList.Draw();
                        break;
                    case Client.State.NotAuthenticated:
                    case Client.State.RetrievingChallenge:
                    case Client.State.WaitingForVerification:
                    case Client.State.Verifying:
                        this.DrawRegistrationPanel();
                        break;
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("设置")) {
                this.DrawSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("帮助")) {
                this.DrawHelp();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void DrawHelp() {
        ImGui.PushTextWrapPos();

        if (ImGui.Button("重置引导")) {
            this.Plugin.ConfigInfo.TutorialStep = 0;
            this.Plugin.SaveConfig();
        }

        ImGui.PopTextWrapPos();
    }

    private void DrawSettings() {
        var anyChanged = false;

        if (ImGui.BeginTabBar("settings-tabs")) {
            if (ImGui.BeginTabItem("通用")) {
                this.DrawSettingsGeneral(ref anyChanged);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("频道")) {
                this.DrawSettingsLinkshells(ref anyChanged);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        if (anyChanged) {
            this.Plugin.SaveConfig();
            this.Plugin.Ipc.BroadcastChannelCommandColours();
        }
    }

    private void DrawSettingsGeneral(ref bool anyChanged) {
        anyChanged |= ImGui.Checkbox("使用系统通知", ref this.Plugin.Config.UseNativeToasts);
        anyChanged |= ImGui.Checkbox("右键菜单添加邀请", ref this.Plugin.Config.ShowContextMenuItem);
        // ImGui.Spacing();
        //
        // ImGui.TextUnformatted("Default channel");
        // ImGui.SetNextItemWidth(-1);
        // if (ImGui.BeginCombo("##default-channel", $"{this.Plugin.Config.DefaultChannel}")) {
        //     foreach (var channel in Enum.GetValues<XivChatType>()) {
        //         if (ImGui.Selectable($"{channel}", this.Plugin.Config.DefaultChannel == channel)) {
        //             this.Plugin.Config.DefaultChannel = channel;
        //             anyChanged = true;
        //         }
        //     }
        //
        //     ImGui.EndCombo();
        // }

        if (this.Plugin.LocalPlayer is { } player) {
            if (ImGui.TreeNodeEx($"{player.Name}{CrossWorld}{player.HomeWorld.Value.Name} 的设置")) {
                if (ImGui.Checkbox("允许接收邀请", ref this.Plugin.ConfigInfo.AllowInvites)) {
                    anyChanged = true;
                    Task.Run(async () => await this.Plugin.Client.AllowInvitesToast(this.Plugin.ConfigInfo.AllowInvites));
                }

                ImGui.TreePop();
            }
        }

        if (this.Plugin.Client.Status == Client.State.Connected && ImGui.TreeNodeEx("删除账户")) {
            ImGui.PushTextWrapPos();

            if (this.Plugin.Client.Channels.Count > 0) {
                ImGui.TextUnformatted("删除账户前，必须先退出或解散所有 ExtraChat 频道。");
            } else {
                ImGui.TextUnformatted("点击下方按钮将永久删除你在 ExtraChat 服务器的账号。此操作不可撤销。");

                if (ImGui.Button("删除账号")) {
                    Task.Run(async () => await this.Plugin.Client.DeleteAccountToast());
                }
            }

            ImGui.PopTextWrapPos();

            ImGui.TreePop();
        }
    }

    private void DrawSettingsLinkshells(ref bool anyChanged) {
        var channelOrder = this.Plugin.ConfigInfo.ChannelOrder.ToDictionary(
            entry => entry.Value,
            entry => entry.Key
        );

        var orderedChannels = this.Plugin.Client.Channels.Keys
            .OrderBy(id => channelOrder.ContainsKey(id) ? channelOrder[id] : int.MaxValue)
            .Concat(this.Plugin.Client.InvitedChannels.Keys);

        foreach (var id in orderedChannels) {
            var name = this.Plugin.ConfigInfo.GetName(id);

            if (ImGui.CollapsingHeader($"{name}###{id}-settings")) {
                ImGui.PushID($"{id}-settings");

                ImGui.TextUnformatted("编号");
                channelOrder.TryGetValue(id, out var refOrder);
                var old = refOrder;
                refOrder += 1;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt("##order", ref refOrder)) {
                    refOrder = Math.Max(1, refOrder) - 1;

                    if (this.Plugin.ConfigInfo.ChannelOrder.TryGetValue(refOrder, out var other) && other != id) {
                        // another channel already has this number, so swap
                        this.Plugin.ConfigInfo.ChannelOrder[old] = other;
                    } else {
                        this.Plugin.ConfigInfo.ChannelOrder.Remove(old);
                    }

                    this.Plugin.ConfigInfo.ChannelOrder[refOrder] = id;
                    anyChanged = true;
                    this.Plugin.Commands.ReregisterAll();
                }

                ImGui.Spacing();

                if (ImGuiUtil.IconButton(FontAwesomeIcon.Undo, "colour-reset", "重置")) {
                    anyChanged = true;
                    this.Plugin.ConfigInfo.ChannelColors.Remove(id);
                }

                ImGui.SameLine();

                var colourKey = this.Plugin.ConfigInfo.GetUiColour(id);
                var colour = this.Plugin.DataManager.GetExcelSheet<UIColor>()!.GetRowOrDefault(colourKey)?.Dark ?? 0xff5ad0ff;
                var vec = ImGui.ColorConvertU32ToFloat4(ColourUtil.RgbaToAbgr(colour));

                const string colourPickerId = "linkshell-colour-picker";

                if (ImGui.ColorButton("Linkshell colour", vec, ImGuiColorEditFlags.NoTooltip)) {
                    ImGui.OpenPopup(colourPickerId);
                }

                ImGui.SameLine();

                ImGui.TextUnformatted("频道颜色");

                if (ImGui.BeginPopup(colourPickerId)) {
                    var i = 0;

                    foreach (var (uiColour, fg) in this._uiColours) {
                        if (ImGui.ColorButton($"Colour {uiColour}", fg, ImGuiColorEditFlags.NoTooltip)) {
                            this.Plugin.ConfigInfo.ChannelColors[id] = (ushort) uiColour;
                            anyChanged = true;
                            ImGui.CloseCurrentPopup();
                        }

                        if (i >= 11) {
                            i = 0;
                        } else {
                            ImGui.SameLine();
                            i += 1;
                        }
                    }

                    ImGui.EndPopup();
                }

                ImGui.Spacing();

                var hint = $"ECLS{refOrder}";
                if (!this.Plugin.ConfigInfo.ChannelMarkers.TryGetValue(id, out var marker)) {
                    marker = string.Empty;
                }

                ImGui.TextUnformatted("聊天标记");
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputTextWithHint("##marker", hint, ref marker, 16)) {
                    anyChanged = true;
                    if (string.IsNullOrWhiteSpace(marker)) {
                        this.Plugin.ConfigInfo.ChannelMarkers.Remove(id);
                    } else {
                        this.Plugin.ConfigInfo.ChannelMarkers[id] = marker;
                    }
                }

                // ImGui.Spacing();
                //
                // ImGui.TextUnformatted("Output channel");
                // ImGui.SetNextItemWidth(-1);
                //
                // var contained = this.Plugin.ConfigInfo.ChannelChannels.TryGetValue(id, out var output);
                // var preview = contained ? $"{output}" : "Default";
                //
                // if (ImGui.BeginCombo("##output-channel", preview)) {
                //     if (ImGui.Selectable("Default", !contained)) {
                //         this.Plugin.ConfigInfo.ChannelChannels.Remove(id);
                //         anyChanged = true;
                //     }
                //
                //     foreach (var channel in Enum.GetValues<XivChatType>()) {
                //         if (ImGui.Selectable($"{channel}", contained && output == channel)) {
                //             this.Plugin.ConfigInfo.ChannelChannels[id] = channel;
                //             anyChanged = true;
                //         }
                //     }
                //
                //     ImGui.EndCombo();
                // }

                ImGui.PopID();
            }
        }
    }

    private void DrawInviteWindow() {
        if (this.InviteInfo == null) {
            return;
        }

        var (name, world) = this.InviteInfo.Value;

        var open = true;
        if (!ImGui.Begin($"邀请: {name}###ec-linkshell-invite", ref open, ImGuiWindowFlags.AlwaysAutoResize)) {
            if (!open) {
                this.InviteInfo = null;
            }

            ImGui.End();
            return;
        }

        if (!open) {
            this.InviteInfo = null;
        }

        if (ImGui.IsWindowAppearing()) {
            ImGui.SetWindowPos(ImGui.GetMousePos());
        }

        var preview = this._inviteId == null ? "选择一个频道" : "???";
        if (this._inviteId != null && this.Plugin.ConfigInfo.Channels.TryGetValue(this._inviteId.Value, out var selectedInfo)) {
            preview = selectedInfo.Name;
        }

        if (ImGui.BeginCombo("##ec-linkshell-invite-linkshell", preview)) {
            foreach (var (id, _) in this.Plugin.Client.Channels) {
                if (!this.Plugin.Client.ChannelRanks.TryGetValue(id, out var rank) || rank < Rank.Moderator) {
                    continue;
                }

                if (!this.Plugin.ConfigInfo.Channels.TryGetValue(id, out var info)) {
                    continue;
                }

                if (ImGui.Selectable($"{info.Name}##{id}", id == this._inviteId)) {
                    this._inviteId = id;
                }
            }

            ImGui.EndCombo();
        }

        if (ImGui.Button("邀请") && this._inviteId != null) {
            var id = this._inviteId.Value;
            this._inviteId = null;

            Task.Run(async () => await this.Plugin.Client.InviteToast(name, world, id));
            this.InviteInfo = null;
        }

        ImGui.End();
    }

    private void DrawRegistrationPanel() {
        if (this.Plugin.LocalPlayer is not { } player) {
            return;
        }

        var state = this.Plugin.Client.Status;
        if (state == Client.State.NotAuthenticated) {
            if (this.Plugin.ConfigInfo.Key != null) {
                ImGui.TextUnformatted("请稍候...");
            } else {
                if (ImGui.Button($"注册 {player.Name}") && !this.Busy) {
                    this.Busy = true;
                    Task.Run(async () => {
                        var challenge = await this.Plugin.Client.GetChallenge();
                        await this._challengeChannel.Writer.WriteAsync(challenge);
                    }).ContinueWith(_ => this.Busy = false);
                }

                ImGui.PushTextWrapPos();
                ImGui.TextUnformatted("ExtraChat 是一个第三方服务，提供跨数据中心的、功能上无限制的额外聊天频道。");
                ImGui.TextUnformatted("ExtraChat 会存储你的角色名、所属服务器，以及你的角色加入和受邀的频道信息。");
                ImGui.TextUnformatted("消息和频道名是端到端加密的，服务器无法解密它们，也不会存储消息内容。");
                ImGui.TextUnformatted("在法律传票的情况下，ExtraChat 将向法律系统提供其可获得的信息。");
                ImGui.PopTextWrapPos();
            }
        }

        if (state == Client.State.RetrievingChallenge) {
            ImGui.TextUnformatted("等待中...");
        }

        if (state == Client.State.WaitingForVerification) {
            ImGui.PushTextWrapPos();
            if (this._challenge == null) {
                ImGui.TextUnformatted("等待验证，但未收到验证码。这是一个 bug。");
            } else {
                ImGui.TextUnformatted("直接点击「验证」按钮即可完成注册（已跳过英雄榜验证）。");

                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##challenge", ref this._challenge, this._challenge.Length, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.ReadOnly);

                if (ImGui.Button("复制")) {
                    ImGui.SetClipboardText(this._challenge);
                }

                ImGui.SameLine();

                if (ImGui.Button("打开石之家")) {
                    Process.Start(new ProcessStartInfo {
                        FileName = "https://ff.web.sdo.com/",
                        UseShellExecute = true,
                    });
                }

                ImGui.SameLine();

                if (ImGui.Button("验证") && !this.Busy) {
                    this.Busy = true;
                    Task.Run(async () => {
                        var key = await this.Plugin.Client.Register();
                        this.Plugin.ConfigInfo.Key = key;
                        this.Plugin.SaveConfig();
                        await this.Plugin.Client.AuthenticateAndList();
                    }).ContinueWith(_ => this.Busy = false);
                }
            }

            ImGui.PopTextWrapPos();
        }
    }
}
