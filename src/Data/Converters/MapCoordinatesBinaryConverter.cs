using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OutsourceTracker.Geolocation;
using System.Runtime.InteropServices;

namespace OutsourceTracker.Data.Converters;

public class MapCoordinatesBinaryConverter : ValueConverter<MapCoordinates?, byte[]>

{
    public MapCoordinatesBinaryConverter() : base(v => v.HasValue ? PositionToBytes(v.Value) : null!, v => v == null ? null : BytesToPosition(v), new ConverterMappingHints(size: 24))
    {
    }

    private static byte[] PositionToBytes(MapCoordinates mapCoordinant)
    {
        byte[] bytes = new byte[8 * 3];
        MemoryMarshal.Write(bytes.AsSpan(0, 8), mapCoordinant.Latitude);
        MemoryMarshal.Write(bytes.AsSpan(8, 8), mapCoordinant.Longitude);
        MemoryMarshal.Write(bytes.AsSpan(16, 8), mapCoordinant.Longitude);
        return bytes;
    }

    private static MapCoordinates? BytesToPosition(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 24)
        {
            return MapCoordinates.Zero;
        }

        double lat = MemoryMarshal.Read<double>(bytes.AsSpan(0, 8));
        double lng = MemoryMarshal.Read<double>(bytes.AsSpan(8, 8));
        double acc = MemoryMarshal.Read<double>(bytes.AsSpan(16, 8));
        return new MapCoordinates(lat, lng, acc);
    }
}
