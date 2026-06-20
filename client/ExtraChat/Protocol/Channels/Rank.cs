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
        Rank.Invited => "\ue070 ",
        Rank.Member => "",
        Rank.Moderator => "\ue0a8 ",
        Rank.Admin => "\ue0a2 ",
        _ => "",
    };
}
