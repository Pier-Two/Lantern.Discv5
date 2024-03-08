namespace Lantern.Discv5.WireProtocol.Utility;

public class CancellationTokenSourceWrapper : ICancellationTokenSourceWrapper, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

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
    
    public void Dispose()
    {
        _cts.Dispose();
        
        // Suppress finalization.
        GC.SuppressFinalize(this);
    }
}