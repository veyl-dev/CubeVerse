using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CubeVerse.Engine
{
    public static class CubeNotation
    {
        private static readonly Regex TokenPattern = new Regex("^(?<width>[0-9]+)?(?<face>[RLUDFBMESrludfbxyz])(?<wide>w)?(?<turn>2|')?$", RegexOptions.Compiled);

        public static bool TryParse(string token, int size, out CubeMove move)
        {
            move = default;
            if (size < 2 || size > 5 || string.IsNullOrWhiteSpace(token)) return false;
            Match match = TokenPattern.Match(token.Trim());
            if (!match.Success) return false;

            string faceToken = match.Groups["face"].Value;
            char face = faceToken[0];
            bool lowerWide = char.IsLower(face) && "rludfb".IndexOf(face) >= 0;
            bool isRotation = "xyz".IndexOf(face) >= 0;
            bool wide = match.Groups["wide"].Success || lowerWide;
            if ((face == 'M' || face == 'E' || face == 'S') && size < 3) return false;
            int width = 1;
            if (match.Groups["width"].Success && !int.TryParse(match.Groups["width"].Value, out width)) return false;
            if (!match.Groups["width"].Success && wide) width = 2;
            if (width < 1 || width > size || (match.Groups["width"].Success && !wide && !isRotation)) return false;
            char f = char.ToUpperInvariant(face);

            CubeAxis axis;
            int min, max, quarter;
            if (f == 'R' || f == 'L')
            {
                axis = CubeAxis.X;
                bool positiveFace = f == 'R';
                min = positiveFace ? size - width : 0;
                max = positiveFace ? size - 1 : width - 1;
                quarter = positiveFace ? -1 : 1;
            }
            else if (f == 'U' || f == 'D')
            {
                axis = CubeAxis.Y;
                bool positiveFace = f == 'U';
                min = positiveFace ? size - width : 0;
                max = positiveFace ? size - 1 : width - 1;
                quarter = positiveFace ? -1 : 1;
            }
            else if (f == 'F' || f == 'B')
            {
                axis = CubeAxis.Z;
                bool positiveFace = f == 'F';
                min = positiveFace ? size - width : 0;
                max = positiveFace ? size - 1 : width - 1;
                quarter = positiveFace ? -1 : 1;
            }
            else if (f == 'M' || f == 'E' || f == 'S')
            {
                axis = f == 'M' ? CubeAxis.X : f == 'E' ? CubeAxis.Y : CubeAxis.Z;
                int center = (size - 1) / 2;
                min = size % 2 == 0 ? center : center;
                max = size % 2 == 0 ? center + 1 : center;
                quarter = f == 'S' ? -1 : 1; // M turns like L; E like D; S like F.
            }
            else
            {
                axis = f == 'X' ? CubeAxis.X : f == 'Y' ? CubeAxis.Y : CubeAxis.Z;
                min = 0;
                max = size - 1;
                quarter = -1; // x like R, y like U, z like F.
                width = size;
            }

            if (isRotation && match.Groups["width"].Success) return false;
            int turns = match.Groups["turn"].Value == "2" ? 2 : quarter;
            if (match.Groups["turn"].Value == "'") turns = -quarter;
            move = new CubeMove(axis, min, max, turns, token.Trim());
            return true;
        }

        public static CubeMove Parse(string token, int size)
        {
            if (!TryParse(token, size, out CubeMove move)) throw new FormatException($"Invalid cube move '{token}' for a {size}×{size} cube.");
            return move;
        }

        public static bool TryParseAlgorithm(string algorithm, int size, out List<CubeMove> moves, out string error)
        {
            moves = new List<CubeMove>();
            error = null;
            if (string.IsNullOrWhiteSpace(algorithm)) return true;
            string[] tokens = Regex.Split(algorithm.Trim(), "[\\s,]+");
            foreach (string token in tokens)
            {
                if (token.Length == 0) continue;
                if (!TryParse(token, size, out CubeMove move))
                {
                    error = $"Unrecognized move '{token}'.";
                    moves.Clear();
                    return false;
                }
                moves.Add(move);
            }
            return true;
        }
    }
}
