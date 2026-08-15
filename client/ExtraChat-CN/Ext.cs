using System.Net.WebSockets;
using ExtraChat.Protocol;
using MessagePack;

namespace ExtraChat;

public static class Ext {
    public static string ToHexString(this IEnumerable<byte> bytes) {
        return string.Join("", bytes.Select(b => b.ToString("x2")));
    }

    public static async Task SendMessage(this ClientWebSocket client, RequestContainer request, CancellationToken token = default) {
        var bytes = MessagePackSerializer.Serialize(request);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        await client.SendAsync(bytes, WebSocketMessageType.Binary, true, cts.Token);
    }

    public static async Task<ResponseContainer> ReceiveMessage(this ClientWebSocket client, CancellationToken token = default) {
        var bytes = new ArraySegment<byte>(new byte[64 * 1024]);

        WebSocketReceiveResult result;
        var i = 0;
        do {
            result = await client.ReceiveAsync(bytes[i..], token);
            i += result.Count;

            if (i >= bytes.Count) {
                // 修复:报文超出 64KB 上限时抛出带类型的异常(而非裸 Exception),
                // 便于调用方区分"超长畸形报文"与普通协议错误;线格式本身不变。
                throw new MessageTooLargeException();
            }
        } while (!result.EndOfMessage);

        return MessagePackSerializer.Deserialize<ResponseContainer>(bytes[..i]);
    }
}

internal sealed class MessageTooLargeException : Exception {
}
