namespace Lantern.Discv5.WireProtocol.Message;

public interface ITalkRequester
{
    byte[] GetProtocol();
    
    byte[] GetTalkRequest();
}