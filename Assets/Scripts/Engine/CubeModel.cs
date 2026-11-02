using System;
using System.Collections.Generic;

namespace CubeVerse.Engine
{
    public sealed class CubeModel
    {
        private readonly CubePiece[] pieces;
        public int Size { get; }
        public IReadOnlyList<CubePiece> Pieces => pieces;

        public CubeModel(int size)
        {
            if (size < 2 || size > 5) throw new ArgumentOutOfRangeException(nameof(size), "Cube size must be between 2 and 5.");
            Size = size;
            pieces = new CubePiece[size * size * size];
            int id = 0;
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            for (int z = 0; z < size; z++)
            {
                var stickers = new List<CubeSticker>(3);
                if (x == size - 1) stickers.Add(new CubeSticker(new Int3(1, 0, 0), StickerColor.Red));
                if (x == 0) stickers.Add(new CubeSticker(new Int3(-1, 0, 0), StickerColor.Orange));
                if (y == size - 1) stickers.Add(new CubeSticker(new Int3(0, 1, 0), StickerColor.White));
                if (y == 0) stickers.Add(new CubeSticker(new Int3(0, -1, 0), StickerColor.Yellow));
                if (z == size - 1) stickers.Add(new CubeSticker(new Int3(0, 0, 1), StickerColor.Green));
                if (z == 0) stickers.Add(new CubeSticker(new Int3(0, 0, -1), StickerColor.Blue));
                var home = new Int3(x, y, z);
                pieces[id] = new CubePiece(id, home, stickers);
                id++;
            }
        }

        public void Reset()
        {
            foreach (CubePiece piece in pieces)
            {
                piece.Position = piece.Home;
                for (int i = 0; i < piece.Stickers.Count; i++)
                {
                    CubeSticker sticker = piece.Stickers[i];
                    sticker.Normal = HomeNormal(piece.Home, sticker.Color);
                    piece.Stickers[i] = sticker;
                }
            }
        }

        public void Apply(CubeMove move)
        {
            if (move.LayerMin < 0 || move.LayerMax >= Size || move.LayerMin > move.LayerMax)
                throw new ArgumentOutOfRangeException(nameof(move), "Move selects a layer outside this cube.");
            int turns = CubeMove.NormalizeTurns(move.QuarterTurns);
            if (turns == 0) return;
            int repeats = Math.Abs(turns);
            int sign = Math.Sign(turns);
            foreach (CubePiece piece in pieces)
            {
                int layer = Coordinate(piece.Position, move.Axis);
                if (!move.Selects(layer)) continue;
                for (int r = 0; r < repeats; r++)
                {
                    piece.Position = RotatePosition(piece.Position, move.Axis, sign, Size);
                    for (int i = 0; i < piece.Stickers.Count; i++)
                    {
                        CubeSticker sticker = piece.Stickers[i];
                        sticker.Normal = RotateVector(sticker.Normal, move.Axis, sign);
                        piece.Stickers[i] = sticker;
                    }
                }
            }
        }

        public void Apply(IEnumerable<CubeMove> moves)
        {
            foreach (CubeMove move in moves) Apply(move);
        }

        public bool ApplyAlgorithm(string algorithm, out string error)
        {
            if (!CubeNotation.TryParseAlgorithm(algorithm, Size, out List<CubeMove> moves, out error)) return false;
            Apply(moves);
            return true;
        }

        public bool IsSolved()
        {
            var faceColors = new Dictionary<Int3, StickerColor>();
            foreach (CubePiece piece in pieces)
            {
                foreach (CubeSticker sticker in piece.Stickers)
                {
                    if (faceColors.TryGetValue(sticker.Normal, out StickerColor faceColor))
                    {
                        if (faceColor != sticker.Color) return false;
                    }
                    else faceColors.Add(sticker.Normal, sticker.Color);
                }
            }
            // A rigid whole-cube rotation changes the view frame, not whether the puzzle is solved.
            return faceColors.Count == 6;
        }

        public CubePiece GetPiece(int id) => pieces[id];

        public List<CubeMove> CreateScramble(int moveCount, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : new Random(Guid.NewGuid().GetHashCode());
            var scramble = new List<CubeMove>(moveCount);
            char previousAxis = '\0';
            char[] faces = { 'R', 'L', 'U', 'D', 'F', 'B' };
            for (int i = 0; i < moveCount; i++)
            {
                char face;
                do { face = faces[rng.Next(faces.Length)]; }
                while (AxisOfFace(face) == previousAxis);
                previousAxis = AxisOfFace(face);
                string suffix = rng.Next(3) == 0 ? "'" : rng.Next(2) == 0 ? string.Empty : "2";
                scramble.Add(CubeNotation.Parse(face + suffix, Size));
            }
            return scramble;
        }

        public static int Coordinate(Int3 p, CubeAxis axis) => axis == CubeAxis.X ? p.x : axis == CubeAxis.Y ? p.y : p.z;

        public static Int3 RotateVector(Int3 v, CubeAxis axis, int sign)
        {
            if (sign == 0) return v;
            for (int i = 0; i < (Math.Abs(sign) % 4); i++)
            {
                if (sign > 0)
                {
                    v = axis == CubeAxis.X ? new Int3(v.x, -v.z, v.y)
                      : axis == CubeAxis.Y ? new Int3(v.z, v.y, -v.x)
                      : new Int3(-v.y, v.x, v.z);
                }
                else
                {
                    v = axis == CubeAxis.X ? new Int3(v.x, v.z, -v.y)
                      : axis == CubeAxis.Y ? new Int3(-v.z, v.y, v.x)
                      : new Int3(v.y, -v.x, v.z);
                }
            }
            return v;
        }

        private static Int3 RotatePosition(Int3 p, CubeAxis axis, int sign, int n)
        {
            if (sign > 0)
            {
                if (axis == CubeAxis.X) return new Int3(p.x, n - 1 - p.z, p.y);
                if (axis == CubeAxis.Y) return new Int3(p.z, p.y, n - 1 - p.x);
                return new Int3(n - 1 - p.y, p.x, p.z);
            }
            if (axis == CubeAxis.X) return new Int3(p.x, p.z, n - 1 - p.y);
            if (axis == CubeAxis.Y) return new Int3(n - 1 - p.z, p.y, p.x);
            return new Int3(p.y, n - 1 - p.x, p.z);
        }

        private static Int3 HomeNormal(Int3 home, StickerColor color)
        {
            switch (color)
            {
                case StickerColor.Red: return new Int3(1, 0, 0);
                case StickerColor.Orange: return new Int3(-1, 0, 0);
                case StickerColor.White: return new Int3(0, 1, 0);
                case StickerColor.Yellow: return new Int3(0, -1, 0);
                case StickerColor.Green: return new Int3(0, 0, 1);
                default: return new Int3(0, 0, -1);
            }
        }

        private static char AxisOfFace(char face) => face == 'R' || face == 'L' ? 'x' : face == 'U' || face == 'D' ? 'y' : 'z';
    }
}
