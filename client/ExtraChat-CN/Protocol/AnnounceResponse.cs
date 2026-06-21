using MessagePack;

namespace ExtraChat.Protocol;

[Serializable]
[MessagePackObject]
public class AnnounceResponse {
    [Key(0)]
    public string Announcement;
}
