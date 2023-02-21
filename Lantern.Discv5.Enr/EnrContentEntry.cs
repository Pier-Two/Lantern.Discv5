namespace Lantern.Discv5.Enr;

public abstract class EnrContentEntry
{
    public abstract string Key { get; }

    public abstract byte[] EncodeEntry();

    public abstract object GetValue();
}

public abstract class EnrContentEntry<TValue> : EnrContentEntry
{
    protected EnrContentEntry(TValue value)
    {
        Value = value;
    }

    public TValue Value { get; }

    public override string ToString()
    {
        return $"{Key} {Value}";
    }

    public override object GetValue()
    {
        return Value;
    }
}