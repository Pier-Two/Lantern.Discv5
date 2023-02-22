namespace Lantern.Discv5.Enr;

public static class Utility
{
    public static int ByteArrayToInt32(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length > 4)
            throw new ArgumentException("Input byte array must be no more than 4 bytes long.", nameof(bytes));

        if (bytes.Length < 4)
        {
            var paddedBytes = new byte[4];
            Buffer.BlockCopy(bytes, 0, paddedBytes, 4 - bytes.Length, bytes.Length);
            bytes = paddedBytes;
        }

        if (IsLittleEndian(bytes)) Array.Reverse(bytes);

        return BitConverter.ToInt32(bytes);
    }

    public static uint ByteArrayToUInt32(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length > 4)
            throw new ArgumentException("Input byte array must be no more than 4 bytes long.", nameof(bytes));

        if (bytes.Length < 4)
        {
            var paddedBytes = new byte[4];
            Buffer.BlockCopy(bytes, 0, paddedBytes, 4 - bytes.Length, bytes.Length);
            bytes = paddedBytes;
        }

        if (IsLittleEndian(bytes)) Array.Reverse(bytes);

        return BitConverter.ToUInt32(bytes);
    }

    public static long ByteArrayToInt64(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length > 8)
            throw new ArgumentException("Input byte array must be no more than 8 bytes long.", nameof(bytes));

        if (bytes.Length < 8)
        {
            var paddedBytes = new byte[8];
            Buffer.BlockCopy(bytes, 0, paddedBytes, 8 - bytes.Length, bytes.Length);
            bytes = paddedBytes;
        }

        if (IsLittleEndian(bytes)) Array.Reverse(bytes);

        return BitConverter.ToInt64(bytes);
    }

    public static ulong ByteArrayToUInt64(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length > 8)
            throw new ArgumentException("Input byte array must be no more than 8 bytes long.", nameof(bytes));

        if (bytes.Length < 8)
        {
            var paddedBytes = new byte[8];
            Buffer.BlockCopy(bytes, 0, paddedBytes, 8 - bytes.Length, bytes.Length);
            bytes = paddedBytes;
        }

        if (IsLittleEndian(bytes)) Array.Reverse(bytes);

        return BitConverter.ToUInt64(bytes);
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

    private static bool IsLittleEndian(byte[] bytes)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length < 2)
            throw new ArgumentException("Input byte array must be at least 2 bytes long.", nameof(bytes));

        return BitConverter.IsLittleEndian == (bytes[0] == 0);
    }
}