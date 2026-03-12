using System;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;

namespace ReadyM.Wukong.Common.ECS.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct EquipmentComponent : INetSerializable
{
    public fixed int Slots[8]; // This is packed inline

    public Span<int> AsSpan()
    {
        fixed (int* ptr = Slots)
        {
            return new Span<int>(ptr, 8);
        }
    }

    public static EquipmentComponent FromSpan(Span<int> span)
    {
        if (span.Length != 8)
            throw new ArgumentException("Span must be of length 8", nameof(span));

        EquipmentComponent equipment = default;
        span.CopyTo(equipment.AsSpan());
        return equipment;
    }

    public void Serialize(NetDataWriter writer)
    {
        for (var i = 0; i < 8; i++)
        {
            writer.Put(Slots[i]);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        for (var i = 0; i < 8; i++)
        {
            Slots[i] = reader.GetInt();
        }
    }
}