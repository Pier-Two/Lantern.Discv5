namespace Lantern.Discv5.Enr;

public static class Utility
{
    public static int ByteArrayToInt32(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length == 0) return 0; 
        
        if (byteArray.Length == 4) return BitConverter.ToInt32(byteArray, 0); 
        
        var paddedArray = new byte[4];
        
        if (byteArray.Length > 4)
            Array.Copy(byteArray, byteArray.Length - 4, paddedArray, 0, 4);
        else
            Array.Copy(byteArray, 0, paddedArray, 4 - byteArray.Length, byteArray.Length);
        
        return BitConverter.ToInt32(paddedArray, 0);
    }

    public static ulong ByteArrayToUInt64(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length == 0) return 0; // return zero
        
        if (byteArray.Length == 8) return BitConverter.ToUInt64(byteArray, 0); // return the converted value
        
        var paddedArray = new byte[8];
        
        if (byteArray.Length > 8)
            Array.Copy(byteArray, byteArray.Length - 8, paddedArray, 0, 8);
        else
            Array.Copy(byteArray, 0, paddedArray, 8 - byteArray.Length, byteArray.Length);
        
        return BitConverter.ToUInt64(paddedArray.Reverse().ToArray(), 0);
    }

    public static int LengthOf(ulong value)
    {
        const int MaxLength = 9;
        const ulong Base = 256L;
        var length = 1;
        var limit = Base;

        while (length < MaxLength && value >= limit)
        {
            length++;
            limit *= Base;
        }

        return length;
    }
}