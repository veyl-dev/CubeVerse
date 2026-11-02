using System;
using System.Collections.Generic;

namespace CubeVerse.Engine
{
    public enum CubeAxis { X, Y, Z }
    public enum StickerColor { White, Yellow, Red, Orange, Blue, Green }

    public struct Int3 : IEquatable<Int3>
    {
        public int x, y, z;
        public Int3(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public static Int3 Zero => new Int3(0, 0, 0);
        public static Int3 operator +(Int3 a, Int3 b) => new Int3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Int3 operator -(Int3 a, Int3 b) => new Int3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Int3 operator -(Int3 a) => new Int3(-a.x, -a.y, -a.z);
        public bool Equals(Int3 other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is Int3 other && Equals(other);
        public override int GetHashCode() => unchecked((x * 73856093) ^ (y * 19349663) ^ (z * 83492791));
        public override string ToString() => $"({x},{y},{z})";
    }

    public struct CubeSticker
    {
        public Int3 Normal;
        public StickerColor Color;
        public CubeSticker(Int3 normal, StickerColor color) { Normal = normal; Color = color; }
    }

    public sealed class CubePiece
    {
        public readonly int Id;
        public readonly Int3 Home;
        public Int3 Position;
        public readonly List<CubeSticker> Stickers;

        internal CubePiece(int id, Int3 home, List<CubeSticker> stickers)
        {
            Id = id;
            Home = home;
            Position = home;
            Stickers = stickers;
        }
    }

    public struct CubeMove
    {
        public CubeAxis Axis;
        public int LayerMin;
        public int LayerMax;
        // Signed quarter turns around the positive world axis. A half turn is +2 or -2.
        public int QuarterTurns;
        public string Notation;

        public CubeMove(CubeAxis axis, int layerMin, int layerMax, int quarterTurns, string notation = null)
        {
            Axis = axis;
            LayerMin = layerMin;
            LayerMax = layerMax;
            QuarterTurns = NormalizeTurns(quarterTurns);
            Notation = notation ?? string.Empty;
        }

        public CubeMove Inverse()
        {
            int inverse = QuarterTurns == 2 ? 2 : -QuarterTurns;
            return new CubeMove(Axis, LayerMin, LayerMax, inverse, string.IsNullOrEmpty(Notation) ? string.Empty : Notation + "⁻¹");
        }

        public bool Selects(int layer) => layer >= LayerMin && layer <= LayerMax;
        public static int NormalizeTurns(int turns)
        {
            int normalized = turns % 4;
            return normalized == -3 ? 1 : normalized == 3 ? -1 : normalized;
        }
    }
}
