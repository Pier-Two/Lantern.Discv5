using System.Security.Cryptography;

namespace Lantern.Discv5.WireProtocol.Utility;

public static class MessageUtils
{
    private const int RequestIdLength = 8;
    
    public static byte[] GenerateRequestId()
    {
        var requestId = new byte[RequestIdLength];
        var random = RandomNumberGenerator.Create(); 
        random.GetBytes(requestId);
        return requestId;
    }
}