namespace ExtraChat.Protocol.Channels;

[Serializable]
public enum Rank : byte {
    Invited = 0,
    Member = 1,
    Moderator = 2,
    Admin = 3,
}

internal static class RankExt {
    internal static string Symbol(this Rank rank) => rank switch {
        Rank.Invited => "[待确认]",
        Rank.Member => "[组员]",
        Rank.Moderator => "[队长]",
        Rank.Admin => "[管理员]",
        _ => "",
    };
}
