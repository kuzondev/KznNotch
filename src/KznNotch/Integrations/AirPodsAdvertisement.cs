namespace KznNotch.Integrations;

// Independently implemented decoder for the observed, undocumented Apple proximity format.
// Only public battery fields are read; encrypted identifiers are neither inspected nor retained.
public sealed record AirPodsAdvertisement(int? Left, int? Right, int? Case,
    bool LeftCharging, bool RightCharging, bool CaseCharging, ushort Model)
{
    public static AirPodsAdvertisement? Decode(ushort company, ReadOnlySpan<byte> bytes)
    {
        if (company != 0x004C) return null;
        for (var offset = 0; offset + 2 <= bytes.Length;)
        {
            var length = bytes[offset + 1];
            if (length > bytes.Length - offset - 2) return null;
            var field = bytes.Slice(offset + 2, length);
            if (bytes[offset] == 7 && length == 25)
            {
                var model = (ushort)(field[1] | field[2] << 8);
                // Headband models use a different battery layout; leave them to Windows/GATT.
                if (model is not (0x2002 or 0x200F or 0x2013 or 0x200E or 0x2014 or 0x2024 or 0x2019 or 0x201B)) return null;
                var currentIsLeft = (field[3] & 0x20) != 0;
                var current = Level(field[4] & 15);
                var other = Level(field[4] >> 4);
                var currentCharging = (field[5] & 0x10) != 0;
                var otherCharging = (field[5] & 0x20) != 0;
                return new(currentIsLeft ? current : other, currentIsLeft ? other : current,
                    Level(field[5] & 15), currentIsLeft ? currentCharging : otherCharging,
                    currentIsLeft ? otherCharging : currentCharging, (field[5] & 0x40) != 0, model);
            }
            offset += length + 2;
        }
        return null;
    }
    private static int? Level(int value) => value <= 10 ? value * 10 : null;
}
