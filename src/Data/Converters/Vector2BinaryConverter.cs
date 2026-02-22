using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OutsourceTracker.Geolocation;

namespace OutsourceTracker.Data.Converters;

public sealed unsafe class Vector2BinaryConverter : ValueConverter<Vector2, byte[]>
{
    private const int SIZE = sizeof(double) * 2;

    public Vector2BinaryConverter() : base(
        v => SerializeTo(v),
        v => DeserializeFrom(v),
        new ConverterMappingHints(size: SIZE))
    {
        
    }

    private static byte[] SerializeTo(Vector2 vector)
    {
        byte[] buffer = new byte[SIZE];

        fixed (byte* ptr = buffer)
        {
            double* dPtr = (double*)ptr;
            dPtr[0] = vector.X;
            dPtr[1] = vector.Y;
        }

        return buffer;
    }

    private static Vector2 DeserializeFrom(byte[] bytes)
    {
        if (bytes is not { Length: SIZE })
            return Vector2.Zero;


        fixed (byte* ptr = bytes)
        {
            double* dPtr = (double*)ptr;
            return new Vector2(dPtr[0], dPtr[1]);
        }
    }
}
