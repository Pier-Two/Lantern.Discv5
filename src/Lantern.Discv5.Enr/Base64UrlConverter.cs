namespace Lantern.Discv5.Enr;

public class Base64UrlConverter
{
    public static string ToBase64UrlString(byte[] bytes)
    {
        var base64String = Convert.ToBase64String(bytes);
        var base64UrlString = base64String.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return base64UrlString;
    }

    public static byte[] FromBase64UrlString(string base64UrlString)
    {
        var base64String = base64UrlString.Replace('-', '+').Replace('_', '/');
        var paddingLength = 4 - (base64String.Length % 4);

        if (paddingLength != 4)
        {
            base64String = base64String.PadRight(base64String.Length + paddingLength, '=');
        }

        var bytes = Convert.FromBase64String(base64String);
        return bytes;
    }
}