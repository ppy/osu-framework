// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Numerics;
using NUnit.Framework;
using osu.Framework.Graphics;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class ColourTest
    {
        [Test]
        public void TestFromHSL()
        {
            // test FromHSL that black and white are only affected by luminance
            testConvertFromHSL(Colour4.White, (0f, 0.5f, 1f, 1f));
            testConvertFromHSL(Colour4.White, (1f, 1f, 1f, 1f));
            testConvertFromHSL(Colour4.White, (0.5f, 0.75f, 1f, 1f));
            testConvertFromHSL(Colour4.Black, (0f, 0.5f, 0f, 1f));
            testConvertFromHSL(Colour4.Black, (1f, 1f, 0f, 1f));
            testConvertFromHSL(Colour4.Black, (0.5f, 0.75f, 0f, 1f));

            // test FromHSL that grey is not affected by hue
            testConvertFromHSL(Colour4.Gray, (0f, 0f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Gray, (0.5f, 0f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Gray, (1f, 0f, 0.5f, 1f));

            // test FromHSL that alpha is being passed through
            testConvertFromHSL(Colour4.Black.Opacity(0.5f), (0f, 0f, 0f, 0.5f));

            // test FromHSL with primaries
            testConvertFromHSL(Colour4.Red, (0, 1f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Yellow, (1f / 6f, 1f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Lime, (2f / 6f, 1f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Cyan, (3f / 6f, 1f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Blue, (4f / 6f, 1f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Magenta, (5f / 6f, 1f, 0.5f, 1f));
            testConvertFromHSL(Colour4.Red, (1f, 1f, 0.5f, 1f));

            // test FromHSL with some other knowns
            testConvertFromHSL(Colour4.CornflowerBlue, (219f / 360f, 0.792f, 0.661f, 1f));
            testConvertFromHSL(Colour4.Tan.Opacity(0.5f), (34f / 360f, 0.437f, 0.686f, 0.5f));
        }

        [Test]
        public void TestToHSL()
        {
            // test ToHSL that black, white, and grey always return constant 0f for hue and saturation
            testConvertToHSL((0f, 0f, 1f, 1f), Colour4.White);
            testConvertToHSL((0f, 0f, 0f, 1f), Colour4.Black);
            testConvertToHSL((0f, 0f, 0.5f, 1f), Colour4.Gray);

            // test ToHSL that alpha is being passed through
            testConvertToHSL((0f, 0f, 0f, 0.5f), Colour4.Black.Opacity(0.5f));

            // test ToHSL with primaries
            testConvertToHSL((0, 1f, 0.5f, 1f), Colour4.Red);
            testConvertToHSL((1f / 6f, 1f, 0.5f, 1f), Colour4.Yellow);
            testConvertToHSL((2f / 6f, 1f, 0.5f, 1f), Colour4.Lime);
            testConvertToHSL((3f / 6f, 1f, 0.5f, 1f), Colour4.Cyan);
            testConvertToHSL((4f / 6f, 1f, 0.5f, 1f), Colour4.Blue);
            testConvertToHSL((5f / 6f, 1f, 0.5f, 1f), Colour4.Magenta);

            // test ToHSL with some other knowns
            testConvertToHSL((219f / 360f, 0.792f, 0.661f, 1f), Colour4.CornflowerBlue);
            testConvertToHSL((34f / 360f, 0.437f, 0.686f, 0.5f), Colour4.Tan.Opacity(0.5f));
        }

        private void testConvertFromHSL(Colour4 expected, (float, float, float, float) convert) =>
            assertAlmostEqual(expected.Vector, Colour4.FromHSL(convert.Item1, convert.Item2, convert.Item3, convert.Item4).Vector);

        private void testConvertToHSL((float, float, float, float) expected, Colour4 convert) =>
            assertAlmostEqual(new Vector4(expected.Item1, expected.Item2, expected.Item3, expected.Item4), convert.ToHSL(), "HSLA");

        [Test]
        public void TestFromHSV()
        {
            // test FromHSV that black is only affected by luminance
            testConvertFromHSV(Colour4.Black, (0f, 0.5f, 0f, 1f));
            testConvertFromHSV(Colour4.Black, (1f, 1f, 0f, 1f));
            testConvertFromHSV(Colour4.Black, (0.5f, 0.75f, 0f, 1f));

            // test FromHSV that white and grey are not affected by hue
            testConvertFromHSV(Colour4.White, (0f, 0f, 1f, 1f));
            testConvertFromHSV(Colour4.White, (1f, 0f, 1f, 1f));
            testConvertFromHSV(Colour4.White, (0.5f, 0f, 1f, 1f));
            testConvertFromHSV(Colour4.Gray, (0f, 0f, 0.5f, 1f));
            testConvertFromHSV(Colour4.Gray, (0.5f, 0f, 0.5f, 1f));
            testConvertFromHSV(Colour4.Gray, (1f, 0f, 0.5f, 1f));

            // test FromHSV that alpha is being passed through
            testConvertFromHSV(Colour4.Black.Opacity(0.5f), (0f, 0f, 0f, 0.5f));

            // test FromHSV with primaries
            testConvertFromHSV(Colour4.Red, (0, 1f, 1f, 1f));
            testConvertFromHSV(Colour4.Yellow, (1f / 6f, 1f, 1f, 1f));
            testConvertFromHSV(Colour4.Lime, (2f / 6f, 1f, 1f, 1f));
            testConvertFromHSV(Colour4.Cyan, (3f / 6f, 1f, 1f, 1f));
            testConvertFromHSV(Colour4.Blue, (4f / 6f, 1f, 1f, 1f));
            testConvertFromHSV(Colour4.Magenta, (5f / 6f, 1f, 1f, 1f));
            testConvertFromHSV(Colour4.Red, (1f, 1f, 1f, 1f));

            // test FromHSV with some other knowns
            testConvertFromHSV(Colour4.CornflowerBlue, (219f / 360f, 0.578f, 0.929f, 1f));
            testConvertFromHSV(Colour4.Tan.Opacity(0.5f), (34f / 360f, 0.333f, 0.824f, 0.5f));
        }

        [Test]
        public void TestToHSV()
        {
            // test ToHSV that black, white, and grey always return constant 0f for hue and saturation
            testConvertToHSV((0f, 0f, 1f, 1f), Colour4.White);
            testConvertToHSV((0f, 0f, 0f, 1f), Colour4.Black);
            testConvertToHSV((0f, 0f, 0.5f, 1f), Colour4.Gray);

            // test ToHSV that alpha is being passed through
            testConvertToHSV((0f, 0f, 1f, 0.5f), Colour4.White.Opacity(0.5f));

            // test ToHSV with primaries
            testConvertToHSV((0, 1f, 1f, 1f), Colour4.Red);
            testConvertToHSV((1f / 6f, 1f, 1f, 1f), Colour4.Yellow);
            testConvertToHSV((2f / 6f, 1f, 1f, 1f), Colour4.Lime);
            testConvertToHSV((3f / 6f, 1f, 1f, 1f), Colour4.Cyan);
            testConvertToHSV((4f / 6f, 1f, 1f, 1f), Colour4.Blue);
            testConvertToHSV((5f / 6f, 1f, 1f, 1f), Colour4.Magenta);

            // test ToHSV with some other knowns
            testConvertToHSV((219f / 360f, 0.578f, 0.929f, 1f), Colour4.CornflowerBlue);
            testConvertToHSV((34f / 360f, 0.333f, 0.824f, 0.5f), Colour4.Tan.Opacity(0.5f));
        }

        private void testConvertFromHSV(Colour4 expected, (float, float, float, float) convert) =>
            assertAlmostEqual(expected.Vector, Colour4.FromHSV(convert.Item1, convert.Item2, convert.Item3, convert.Item4).Vector);

        private void testConvertToHSV((float, float, float, float) expected, Colour4 convert) =>
            assertAlmostEqual(new Vector4(expected.Item1, expected.Item2, expected.Item3, expected.Item4), convert.ToHSV(), "HSVA");

        [Test]
        public void TestToHex()
        {
            Assert.AreEqual("#D2B48C", Colour4.Tan.ToHex());
            Assert.AreEqual("#D2B48CFF", Colour4.Tan.ToHex(true));
            Assert.AreEqual("#6495ED80", Colour4.CornflowerBlue.Opacity(half_alpha).ToHex());
        }

        private static readonly object[][] valid_hex_colours =
        {
            new object[] { Colour4.White, "#fff" },
            new object[] { Colour4.Red, "#ff0000" },
            new object[] { Colour4.Yellow.Opacity(half_alpha), "ffff0080" },
            new object[] { Colour4.Lime.Opacity(half_alpha), "00ff0080" },
            new object[] { new Colour4(17, 34, 51, 255), "123" },
            new object[] { new Colour4(17, 34, 51, 255), "#123" },
            new object[] { new Colour4(17, 34, 51, 68), "1234" },
            new object[] { new Colour4(17, 34, 51, 68), "#1234" },
            new object[] { new Colour4(18, 52, 86, 255), "123456" },
            new object[] { new Colour4(18, 52, 86, 255), "#123456" },
            new object[] { new Colour4(18, 52, 86, 120), "12345678" },
            new object[] { new Colour4(18, 52, 86, 120), "#12345678" }
        };

        [TestCaseSource(nameof(valid_hex_colours))]
        public void TestFromHex(Colour4 expectedColour, string hexCode)
        {
            Assert.AreEqual(expectedColour, Colour4.FromHex(hexCode));

            Assert.True(Colour4.TryParseHex(hexCode, out var actualColour));
            Assert.AreEqual(expectedColour, actualColour);
        }

        [TestCase("1")]
        [TestCase("#1")]
        [TestCase("12")]
        [TestCase("#12")]
        [TestCase("12345")]
        [TestCase("#12345")]
        [TestCase("1234567")]
        [TestCase("#1234567")]
        [TestCase("123456789")]
        [TestCase("#123456789")]
        [TestCase("gg00zz")]
        public void TestFromHexFailsOnInvalidColours(string invalidColour)
        {
            // Assert.Catch allows any exception type, contrary to .Throws<T>() (which expects exactly T)
            Assert.Catch(() => Colour4.FromHex(invalidColour));

            Assert.False(Colour4.TryParseHex(invalidColour, out _));
        }

        [Test]
        public void TestChainingFunctions()
        {
            // test that Opacity replaces alpha channel rather than multiplying
            var expected1 = new Colour4(1f, 0f, 0f, 0.5f);
            Assert.AreEqual(expected1, Colour4.Red.Opacity(0.5f));
            Assert.AreEqual(expected1, expected1.Opacity(0.5f));

            // test that MultiplyAlpha multiplies existing alpha channel
            var expected2 = new Colour4(1f, 0f, 0f, 0.25f);
            Assert.AreEqual(expected2, expected1.MultiplyAlpha(0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Colour4.White.MultiplyAlpha(-1f));

            // test clamping all channels in either direction
            Assert.AreEqual(Colour4.White, new Colour4(1.1f, 1.1f, 1.1f, 1.1f).Clamped());
            Assert.AreEqual(Colour4.Black.Opacity(0f), new Colour4(-1.1f, -1.1f, -1.1f, -1.1f).Clamped());

            // test lighten and darken
            assertAlmostEqual(new Colour4(0.431f, 0.642f, 1f, 1f).Vector, Colour4.CornflowerBlue.Lighten(0.1f).Vector);
            assertAlmostEqual(new Colour4(0.356f, 0.531f, 0.845f, 1f).Vector, Colour4.CornflowerBlue.Darken(0.1f).Vector);
        }

        [Test]
        public void TestOperators()
        {
            var colour = new Colour4(0.5f, 0.5f, 0.5f, 0.5f);
            assertAlmostEqual(new Vector4(0.6f, 0.7f, 0.8f, 0.9f), (colour + new Colour4(0.1f, 0.2f, 0.3f, 0.4f)).Vector);
            assertAlmostEqual(new Vector4(0.4f, 0.3f, 0.2f, 0.1f), (colour - new Colour4(0.1f, 0.2f, 0.3f, 0.4f)).Vector);
            assertAlmostEqual(new Vector4(0.25f, 0.25f, 0.25f, 0.25f), (colour * colour).Vector);
            assertAlmostEqual(new Vector4(0.25f, 0.25f, 0.25f, 0.25f), (colour / 2f).Vector);
            assertAlmostEqual(Colour4.White.Vector, (colour * 2f).Vector);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = colour * -1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = colour / -1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = colour / 0f);
        }

        [Test]
        public void TestOtherConversions()
        {
            // test uint conversions
            Assert.AreEqual(0x6495ED80, Colour4.CornflowerBlue.Opacity(half_alpha).ToRGBA());
            Assert.AreEqual(0x806495ED, Colour4.CornflowerBlue.Opacity(half_alpha).ToARGB());
            Assert.AreEqual(Colour4.CornflowerBlue.Opacity(half_alpha), Colour4.FromRGBA(0x6495ED80));
            Assert.AreEqual(Colour4.CornflowerBlue.Opacity(half_alpha), Colour4.FromARGB(0x806495ED));

            // test SRGB
            var srgb = new Vector4(0.659f, 0.788f, 0.968f, 1f);
            assertAlmostEqual(srgb, Colour4.CornflowerBlue.ToSRGB().Vector);
            assertAlmostEqual(Colour4.CornflowerBlue.Vector, new Colour4(srgb).ToLinear().Vector);
        }

        private void assertAlmostEqual(Vector4 expected, Vector4 actual, string type = "RGBA")
        {
            // note that we use a fairly high delta since the test constants are approximations
            const float delta = 0.005f;
            string message = $"({type}) Expected: {expected}, Actual: {actual}";
            Assert.AreEqual(expected.X, actual.X, delta, message);
            Assert.AreEqual(expected.Y, actual.Y, delta, message);
            Assert.AreEqual(expected.Z, actual.Z, delta, message);
            Assert.AreEqual(expected.W, actual.W, delta, message);
        }

        // 0x80 alpha is slightly more than half
        private const float half_alpha = 128f / 255f;
    }
}
