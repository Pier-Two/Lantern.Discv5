namespace Lantern.Discv5.WireProtocol.Utility;

public class CancellationTokenSourceWrapper : ICancellationTokenSourceWrapper
{
    private readonly CancellationTokenSource _cts;

    public CancellationTokenSourceWrapper()
    {
        _cts = new CancellationTokenSource();
    }
    
    public CancellationToken GetToken()
    {
        return _cts.Token;
    }
    
    public void Cancel()
    {
        _cts.Cancel();
    }
    
    public bool IsCancellationRequested()
    {
        return _cts.IsCancellationRequested;
    }
}