namespace Lantern.Discv5.WireProtocol.Message;

public interface ITalkRequester
{
    public byte[] GetProtocol();
    
    public byte[] GetTalkRequest();
}