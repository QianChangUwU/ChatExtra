using MessagePack;

namespace ExtraChat.Protocol;

[Serializable]
[MessagePackObject]
public class NicknameResponse {
    [Key(0)]
    public string? Nickname;
}
