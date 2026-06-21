using MessagePack;

namespace ExtraChat.Protocol;

[Serializable]
[MessagePackObject]
public class NicknameRequest {
    [Key(0)]
    public string? Nickname;
}
