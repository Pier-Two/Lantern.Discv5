namespace Lantern.Discv5.WireProtocol.Messages;

public static class MessageUtility
{
    static Random random = new Random();

    public static byte[] GenerateRequestId(int requestIdLength)
    {
        var requestId = new byte[requestIdLength];
        random.NextBytes(requestId);
        return requestId;
    }
}
