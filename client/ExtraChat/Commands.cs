using Dalamud.Game.Command;
using ExtraChat.Util;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace ExtraChat;

internal class Commands : IDisposable {
    private static readonly string[] MainCommands = {
        "/extrachat",
        "/ec",
        "/eclcmd",
    };

    private Plugin Plugin { get; }
    private Dictionary<string, Guid> RegisteredInternal { get; } = new();
    internal IReadOnlyDictionary<string, Guid> Registered => this.RegisteredInternal;

    internal Commands(Plugin plugin) {
        this.Plugin = plugin;
        this.Plugin.ClientState.Logout += this.OnLogout;

        this.RegisterMain();
        this.RegisterAll();
    }

    private void OnLogout(int type, int code) {
        this.UnregisterAll();
    }

    private void RegisterMain() {
        foreach (var command in MainCommands) {
            this.Plugin.CommandManager.AddHandler(command, new CommandInfo(this.MainCommand) {
                HelpMessage = "打开 ExtraChat 主界面。使用 /extrachat server <url> 切换服务器。",
            });
        }
    }

    private void UnregisterMain() {
        foreach (var command in MainCommands) {
            this.Plugin.CommandManager.RemoveHandler(command);
        }
    }

    private void MainCommand(string command, string arguments) {
        var args = arguments.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0) {
            this.Plugin.PluginUi.Visible ^= true;
            return;
        }

        switch (args[0]) {
            case "server": {
                if (args.Length < 2) {
                    this.Plugin.ChatGui.Print(new XivChatEntry {
                        Message = $"ExtraChat 服务器：{this.Plugin.ConfigInfo.ServerUrl}",
                        Type = XivChatType.Notice,
                    });
                    return;
                }

                var url = args[1];
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "ws" && uri.Scheme != "wss")) {
                    this.Plugin.ChatGui.Print(new XivChatEntry {
                        Message = "无效的 URL，必须以 ws:// 或 wss:// 开头",
                        Type = XivChatType.Urgent,
                    });
                    return;
                }

                this.Plugin.ConfigInfo.ServerUrl = url;
                this.Plugin.SaveConfig();
                this.Plugin.Client.StopLoop();
                this.Plugin.Client.StartLoop();

                this.Plugin.ChatGui.Print(new XivChatEntry {
                    Message = $"ExtraChat 服务器已切换为 {url}",
                    Type = XivChatType.Notice,
                });
                break;
            }
            default: {
                this.Plugin.PluginUi.Visible ^= true;
                break;
            }
        }
    }

    internal void ReregisterAll() {
        this.UnregisterAll();
        this.RegisterAll();
        this.Plugin.Ipc.BroadcastChannelCommandColours();
    }

    internal void RegisterAll() {
        var info = this.Plugin.ConfigInfo;
        foreach (var (idx, id) in info.ChannelOrder) {
            this.RegisterOne($"/ecl{idx + 1}", id);
        }

        foreach (var (alias, id) in info.Aliases) {
            this.RegisterOne(alias, id);
        }
    }

    internal void UnregisterAll() {
        foreach (var command in this.Registered.Keys) {
            this.Plugin.CommandManager.RemoveHandler(command);
        }

        this.RegisteredInternal.Clear();
    }

    private void RegisterOne(string command, Guid id) {
        this.RegisteredInternal[command] = id;

        void Handler(string _, string arguments) {
            Plugin.Log.Warning("Command handler actually invoked");
        }

        this.Plugin.CommandManager.AddHandler(command, new CommandInfo(Handler) {
            ShowInHelp = false,
        });
    }

    internal void SendMessage(Guid id, byte[] bytes) {
        if (!this.Plugin.ConfigInfo.Channels.TryGetValue(id, out var info)) {
            this.Plugin.ChatGui.PrintError("ExtraChat Linkshell information could not be loaded.");
            return;
        }

        var message = this.Plugin.GameFunctions.ResolvePayloads(bytes);
        var ciphertext = SecretBox.Encrypt(info.SharedSecret, message);
        Task.Run(async () => await this.Plugin.Client.SendMessage(id, ciphertext));
    }

    public void Dispose() {
        this.UnregisterAll();
        this.UnregisterMain();

        this.Plugin.ClientState.Logout -= this.OnLogout;
    }
}
