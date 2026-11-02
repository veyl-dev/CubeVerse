using System.Collections.Generic;
using System.Linq;
using CubeVerse.Engine;
using CubeVerse.Gameplay;
using NUnit.Framework;

namespace CubeVerse.Tests
{
    public sealed class CubeModelTests
    {
        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void NewCubeIsSolvedAndHasExpectedStickerCount(int size)
        {
            var cube = new CubeModel(size);
            Assert.That(cube.IsSolved(), Is.True);
            Assert.That(cube.Pieces.Count, Is.EqualTo(size * size * size));
            Assert.That(cube.Pieces.Sum(p => p.Stickers.Count), Is.EqualTo(6 * size * size));
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void EachOuterFaceTurnAndItsInverseRestoreSolvedState(int size)
        {
            foreach (string token in new[] { "R", "L", "U", "D", "F", "B" })
            {
                var cube = new CubeModel(size);
                CubeMove move = CubeNotation.Parse(token, size);
                cube.Apply(move);
                Assert.That(cube.IsSolved(), Is.False, token + " must change the cube.");
                cube.Apply(move.Inverse());
                Assert.That(cube.IsSolved(), Is.True, token + " followed by its inverse must solve.");
            }
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void FourQuarterTurnsRestoreEveryPieceAndSticker(int size)
        {
            foreach (string token in new[] { "R", "L", "U", "D", "F", "B" })
            {
                var cube = new CubeModel(size);
                CubeMove move = CubeNotation.Parse(token, size);
                for (int i = 0; i < 4; i++) cube.Apply(move);
                Assert.That(cube.IsSolved(), Is.True, token + "^4");
            }
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void SeededScrambleAndInverseAreAValidRoundTrip(int size)
        {
            var cube = new CubeModel(size);
            List<CubeMove> scramble = cube.CreateScramble(60, 183);
            Assert.That(scramble.Count, Is.EqualTo(60));
            Assert.That(scramble.Zip(scramble.Skip(1), (a, b) => a.Axis != b.Axis).All(x => x), Is.True);
            cube.Apply(scramble);
            Assert.That(cube.IsSolved(), Is.False);
            foreach (CubeMove move in scramble.AsEnumerable().Reverse()) cube.Apply(move.Inverse());
            Assert.That(cube.IsSolved(), Is.True);
        }

        [TestCase(3, "Rw", 2)] [TestCase(4, "3Uw", 3)] [TestCase(5, "r'", 2)]
        public void WideMovesSelectTheExpectedLayerCount(int size, string token, int width)
        {
            CubeMove move = CubeNotation.Parse(token, size);
            Assert.That(move.LayerMax - move.LayerMin + 1, Is.EqualTo(width));
            var cube = new CubeModel(size);
            cube.Apply(move);
            cube.Apply(move.Inverse());
            Assert.That(cube.IsSolved(), Is.True);
        }

        [TestCase(3, "M")]
        [TestCase(4, "E2")]
        [TestCase(5, "S'")]
        [TestCase(3, "x")]
        [TestCase(4, "y2")]
        [TestCase(5, "z'")]
        public void SliceAndWholeCubeNotationRoundTrips(int size, string token)
        {
            var cube = new CubeModel(size);
            CubeMove move = CubeNotation.Parse(token, size);
            cube.Apply(move);
            cube.Apply(move.Inverse());
            Assert.That(cube.IsSolved(), Is.True, token);
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void WholeCubeReorientationDoesNotMakeAUniformSolvedCubeUnsolved(int size)
        {
            var cube = new CubeModel(size);
            foreach (string token in new[] { "x", "y", "z'" }) cube.Apply(CubeNotation.Parse(token, size));
            Assert.That(cube.IsSolved(), Is.True);
        }

        [Test]
        public void AlgorithmParserRejectsInvalidInputWithoutPartiallyApplying()
        {
            var cube = new CubeModel(3);
            Assert.That(cube.ApplyAlgorithm("R U definitely-not-a-move", out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(cube.IsSolved(), Is.True);
        }

        [Test]
        public void TrimmedAverageOfFiveDropsOneFastestAndOneSlowest()
        {
            Assert.That(LocalStatistics.AverageOfFive(new[] { 8f, 10f, 9f, 11f, 100f }), Is.EqualTo(10f).Within(0.001f));
        }
    }
}
