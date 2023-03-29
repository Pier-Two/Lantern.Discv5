namespace Lantern.Discv5.Enr.Content;

public class EnrContentKey
{
    private EnrContentKey(string value) => Value = value;
    
    public string Value { get; }
    
    public static readonly EnrContentKey Attnets = new("attnets");
    
    public static readonly EnrContentKey Eth2 = new("eth2");
    
    public static readonly EnrContentKey Id = new("id");
    
    public static readonly EnrContentKey Syncnets = new("syncnets");
    
    public static readonly EnrContentKey Ip = new("ip");
    
    public static readonly EnrContentKey Ip6 = new("ip6");
    
    public static readonly EnrContentKey Secp256K1 = new("secp256k1");
    
    public static readonly EnrContentKey Tcp = new("tcp");
    
    public static readonly EnrContentKey Tcp6 = new("tcp6");
    
    public static readonly EnrContentKey Udp = new("udp");
    
    public static readonly EnrContentKey Udp6 = new("udp6");
    
    public static implicit operator string(EnrContentKey key) => key.Value;
    
    public static implicit operator EnrContentKey(string key) => new(key);
    
    public override string ToString() => Value;
    
    public override bool Equals(object? obj)
    {
        return obj is EnrContentKey other && Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}