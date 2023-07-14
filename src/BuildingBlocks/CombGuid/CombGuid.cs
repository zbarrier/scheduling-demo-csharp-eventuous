using MassTransit;
using MassTransit.NewIdProviders;

namespace BuildingBlocks.CombGuid;
public class CombGuid
{
    public static void Initialize()
    {
        NewId.SetProcessIdProvider(new CurrentProcessIdProvider());
    }

    public static Guid Next()
    {
        var newId = NewId.Next();
        return ConvertSqlServerOrderedToLexicalOrdered(newId);
    }

    public static Guid ConvertSqlServerOrderedToLexicalOrdered(NewId newId)
    {
        var bytes = newId.ToByteArray();

        var a = (int)(bytes[10] << 24 | bytes[11] << 16 | bytes[12] << 8 | bytes[13]);
        var b = (short)(bytes[14] << 8 | bytes[15]);
        var c = (short)(bytes[8] << 8 | bytes[9]);

        return new Guid(a, b, c, bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[0], bytes[1]);
    }

    public static Guid ConvertLexicalOrderedToSqlServerOrdered(NewId guidId)
    {
        var bytes = guidId.ToByteArray();

        var a = (int)(bytes[12] << 24 | bytes[13] << 16 | bytes[15] << 8 | bytes[14]);
        var b = (short)(bytes[10] << 8 | bytes[11]);
        var c = (short)(bytes[8] << 8 | bytes[9]);

        return new Guid(a, b, c, bytes[7], bytes[6], bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4]);
    }
}
