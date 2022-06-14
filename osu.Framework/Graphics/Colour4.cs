// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Globalization;
using System.Numerics;
using osuTK.Graphics;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Represents an RGBA colour in the linear colour space, having colour components in the range 0-1.
    /// Stored internally as a <see cref="Vector4"/> for performance.
    /// </summary>
    public readonly struct Colour4 : IEquatable<Colour4>
    {
        /// <summary>
        /// <see cref="Vector4"/> representation of the colour, where XYZW maps to RGBA,
        /// and each component is in the 0-1 range.
        /// </summary>
        public readonly Vector4 Vector;

        /// <summary>
        /// Represents the red component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public float R => Vector.X;

        /// <summary>
        /// Represents the green component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public float G => Vector.Y;

        /// <summary>
        /// Represents the blue component of the linear RGBA colour in the 0-1 range.
        /// </summary>
        public float B => Vector.Z;

        /// <summary>
        /// Represents the alpha component of the RGBA colour in the 0-1 range.
        /// </summary>
        public float A => Vector.W;

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="Colour4"/> with the specified RGBA components in the 0-1 range.
        /// </summary>
        /// <param name="r">The red component, in the 0-1 range.</param>
        /// <param name="g">The green component, in the 0-1 range.</param>
        /// <param name="b">The blue component, in the 0-1 range.</param>
        /// <param name="a">The alpha component, in the 0-1 range.</param>
        public Colour4(float r, float g, float b, float a)
        {
            Vector = new Vector4(r, g, b, a);
        }

        /// <summary>
        /// Creates a new <see cref="Colour4"/> with the specified RGBA components in the 0-255 range.
        /// </summary>
        /// <param name="r">The red component, in the 0-255 range.</param>
        /// <param name="g">The green component, in the 0-255 range.</param>
        /// <param name="b">The blue component, in the 0-255 range.</param>
        /// <param name="a">The alpha component, in the 0-255 range.</param>
        public Colour4(byte r, byte g, byte b, byte a)
        {
            Vector = new Vector4(
                r / (float)byte.MaxValue,
                g / (float)byte.MaxValue,
                b / (float)byte.MaxValue,
                a / (float)byte.MaxValue);
        }

        /// <summary>
        /// Creates a new <see cref="Colour4"/> with the specified <see cref="Vector4"/>,
        /// where XYZW maps to RGBA.
        /// </summary>
        /// <param name="vector">The source vector, whose components should be in the 0-1 range.</param>
        public Colour4(Vector4 vector)
        {
            Vector = vector;
        }

        #endregion

        #region Chaining Functions

        /// <summary>
        /// Returns a new <see cref="Colour4"/> with the same RGB components, but multiplying the current alpha component by a scalar value.
        /// The final alpha is clamped to the 0-1 range.
        /// </summary>
        /// <param name="scalar">The value that the existing alpha will be multiplied by.</param>
        public Colour4 MultiplyAlpha(float scalar)
        {
            if (scalar < 0)
                throw new ArgumentOutOfRangeException(nameof(scalar), scalar, "Cannot multiply alpha by a negative value.");

            return new Colour4(R, G, B, Math.Min(1f, A * scalar));
        }

        /// <summary>
        /// Returns a new <see cref="Colour4"/> with the same RGB components and a specified alpha value.
        /// The final alpha is clamped to the 0-1 range.
        /// </summary>
        /// <param name="alpha">The new alpha value for the returned colour, in the 0-1 range.</param>
        public Colour4 Opacity(float alpha) => new Colour4(R, G, B, Math.Clamp(alpha, 0f, 1f));

        /// <summary>
        /// Returns a new <see cref="Colour4"/> with the same RGB components and a specified alpha value.
        /// The final alpha is clamped to the 0-1 range.
        /// </summary>
        /// <param name="alpha">The new alpha value for the returned colour, in the 0-255 range.</param>
        public Colour4 Opacity(byte alpha) => new Colour4(R, G, B, Math.Clamp(alpha / (float)byte.MaxValue, 0f, 1f));

        /// <summary>
        /// Returns a new <see cref="Colour4"/> with its individual components clamped to the 0-1 range.
        /// </summary>
        public Colour4 Clamped() => new Colour4(Vector4.Clamp(Vector, Vector4.Zero, Vector4.One));

        /// <summary>
        /// Returns a lightened version of the colour.
        /// </summary>
        /// <param name="amount">Percentage light addition</param>
        public Colour4 Lighten(float amount)
        {
            float scalar = Math.Max(1f, 1f + amount);
            return new Colour4(R * scalar, G * scalar, B * scalar, A).Clamped();
        }

        /// <summary>
        /// Returns a darkened version of the colour.
        /// </summary>
        /// <param name="amount">Percentage light reduction</param>
        public Colour4 Darken(float amount)
        {
            float scalar = Math.Max(1f, 1f + amount);
            return new Colour4(R / scalar, G / scalar, B / scalar, A).Clamped();
        }

        #endregion

        #region Operator Overloads

        /// <summary>
        /// Multiplies two colours in the linear colour space.
        /// </summary>
        /// <param name="first">The left hand side of the multiplication.</param>
        /// <param name="second">The right hand side of the multiplication.</param>
        public static Colour4 operator *(Colour4 first, Colour4 second) =>
            new Colour4(first.Vector * second.Vector);

        /// <summary>
        /// Adds two colours in the linear colour space. The final value is clamped to the 0-1 range.
        /// </summary>
        /// <param name="first">The left hand side of the addition.</param>
        /// <param name="second">The right hand side of the addition.</param>
        public static Colour4 operator +(Colour4 first, Colour4 second) =>
            new Colour4(Vector4.Min(first.Vector + second.Vector, Vector4.One));

        /// <summary>
        /// Subtracts two colours in the linear colour space. The final value is clamped to the 0-1 range.
        /// </summary>
        /// <param name="first">The left hand side of the subtraction.</param>
        /// <param name="second">The right hand side of the subtraction.</param>
        public static Colour4 operator -(Colour4 first, Colour4 second) =>
            new Colour4(Vector4.Max(first.Vector - second.Vector, Vector4.Zero));

        /// <summary>
        /// Linearly multiplies a colour by a scalar value. The final value is clamped to the 0-1 range.
        /// </summary>
        /// <param name="colour">The original colour.</param>
        /// <param name="scalar">The scalar value to multiply by. Must not be negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="scalar"/> is negative.</exception>
        public static Colour4 operator *(Colour4 colour, float scalar)
        {
            if (scalar < 0)
                throw new ArgumentOutOfRangeException(nameof(scalar), scalar, "Cannot multiply colours by negative values.");

            return new Colour4(Vector4.Min(colour.Vector * scalar, Vector4.One));
        }

        /// <summary>
        /// Linearly divides a colour by a scalar value. The final value is clamped to the 0-1 range.
        /// </summary>
        /// <param name="colour">The original colour.</param>
        /// <param name="scalar">The scalar value to divide by. Must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="scalar"/> is zero or negative.</exception>
        public static Colour4 operator /(Colour4 colour, float scalar)
        {
            if (scalar <= 0)
                throw new ArgumentOutOfRangeException(nameof(scalar), scalar, "Cannot divide colours by non-positive values.");

            return colour * (1 / scalar);
        }

        /// <summary>
        /// Performs a <see cref="Colour4"/> equality check using the <see cref="IEquatable{T}"/> implementation.
        /// </summary>
        /// <param name="first">The left hand side of the equation.</param>
        /// <param name="second">The right hand side of the equation.</param>
        public static bool operator ==(Colour4 first, Colour4 second) => first.Equals(second);

        /// <summary>
        /// Performs a <see cref="Colour4"/> inequality check using the <see cref="IEquatable{T}"/> implementation.
        /// </summary>
        /// <param name="first">The left hand side of the equation.</param>
        /// <param name="second">The right hand side of the equation.</param>
        public static bool operator !=(Colour4 first, Colour4 second) => !first.Equals(second);

        /// <summary>
        /// Converts an osuTK <see cref="Color4"/> to an osu!framework <see cref="Colour4"/>.
        /// </summary>
        /// <param name="colour">The osuTK <see cref="Color4"/> to convert.</param>
        public static implicit operator Colour4(Color4 colour) =>
            new Colour4(colour.R, colour.G, colour.B, colour.A);

        /// <summary>
        /// Converts an osu!framework <see cref="Colour4"/> to an osuTK <see cref="Color4"/>.
        /// </summary>
        /// <param name="colour">The osu!framework <see cref="Colour4"/> to convert.</param>
        public static implicit operator Color4(Colour4 colour) =>
            new Color4(colour.R, colour.G, colour.B, colour.A);

        #endregion

        #region Conversion

        /// <summary>
        /// Returns a new <see cref="Colour4"/> with an SRGB->Linear conversion applied
        /// to each of its chromatic components. Alpha is unchanged.
        /// </summary>
        public Colour4 ToLinear() => new Colour4((float)toLinear(R), (float)toLinear(G), (float)toLinear(B), A);

        /// <summary>
        /// Returns a new <see cref="Colour4"/> with a Linear->SRGB conversion applied
        /// to each of its chromatic components. Alpha is unchanged.
        /// </summary>
        public Colour4 ToSRGB() => new Colour4((float)toSRGB(R), (float)toSRGB(G), (float)toSRGB(B), A);

        /// <summary>
        /// Returns the <see cref="Colour4"/> as a 32-bit unsigned integer in the format RGBA.
        /// </summary>
        public uint ToRGBA() => ((uint)(Math.Min(1f, R) * byte.MaxValue) << 24) |
                                ((uint)(Math.Min(1f, G) * byte.MaxValue) << 16) |
                                ((uint)(Math.Min(1f, B) * byte.MaxValue) << 8) |
                                (uint)(Math.Min(1f, A) * byte.MaxValue);

        /// <summary>
        /// Returns a new <see cref="Colour4"/> from the passed 32-bit unsigned integer in the format RGBA.
        /// </summary>
        /// <param name="rgba">The source colour in Rgba32 format.</param>
        public static Colour4 FromRGBA(uint rgba) => new Colour4((byte)((rgba >> 24) & 0xff), (byte)((rgba >> 16) & 0xff), (byte)((rgba >> 8) & 0xff), (byte)(rgba & 0xff));

        /// <summary>
        /// Returns the <see cref="Colour4"/> as a 32-bit unsigned integer in the format ARGB.
        /// </summary>
        public uint ToARGB() => ((uint)(Math.Min(1f, A) * byte.MaxValue) << 24) |
                                ((uint)(Math.Min(1f, R) * byte.MaxValue) << 16) |
                                ((uint)(Math.Min(1f, G) * byte.MaxValue) << 8) |
                                (uint)(Math.Min(1f, B) * byte.MaxValue);

        /// <summary>
        /// Returns a new <see cref="Colour4"/> from the passed 32-bit unsigned integer in the format ARGB.
        /// </summary>
        /// <param name="argb">The source colour in Argb32 format.</param>
        public static Colour4 FromARGB(uint argb) => new Colour4((byte)((argb >> 16) & 0xff), (byte)((argb >> 8) & 0xff), (byte)(argb & 0xff), (byte)((argb >> 24) & 0xff));

        /// <summary>
        /// Converts an RGB or RGBA-formatted hex colour code into a <see cref="Colour4"/>.
        /// Supported colour code formats:
        /// <list type="bullet">
        /// <item><description>RGB</description></item>
        /// <item><description>#RGB</description></item>
        /// <item><description>RGBA</description></item>
        /// <item><description>#RGBA</description></item>
        /// <item><description>RRGGBB</description></item>
        /// <item><description>#RRGGBB</description></item>
        /// <item><description>RRGGBBAA</description></item>
        /// <item><description>#RRGGBBAA</description></item>
        /// </list>
        /// </summary>
        /// <param name="hex">The hex code.</param>
        /// <returns>The <see cref="Colour4"/> representing the colour.</returns>
        /// <exception cref="ArgumentException">If <paramref name="hex"/> is not a supported colour code.</exception>
        public static Colour4 FromHex(string hex)
        {
            if (!TryParseHex(hex, out var colour))
                throw new ArgumentException($"{hex} is not a valid colour hex string.", nameof(hex));

            return colour;
        }

        /// <summary>
        /// Attempts to convert an RGB or RGBA-formatted hex colour code into a <see cref="Colour4"/>.
        /// Supported colour code formats:
        /// <list type="bullet">
        /// <item><description>RGB</description></item>
        /// <item><description>#RGB</description></item>
        /// <item><description>RGBA</description></item>
        /// <item><description>#RGBA</description></item>
        /// <item><description>RRGGBB</description></item>
        /// <item><description>#RRGGBB</description></item>
        /// <item><description>RRGGBBAA</description></item>
        /// <item><description>#RRGGBBAA</description></item>
        /// </list>
        /// </summary>
        /// <param name="hex">The hex code.</param>
        /// <param name="colour">The <see cref="Colour4"/> representing the colour, if parsing succeeded.</param>
        /// <returns>Whether the input could be parsed as a hex code.</returns>
        public static bool TryParseHex(string hex, out Colour4 colour)
        {
            var hexSpan = hex.StartsWith('#') ? hex.AsSpan(1) : hex.AsSpan();

            bool parsed = true;
            byte r = 255, g = 255, b = 255, a = 255;

            switch (hexSpan.Length)
            {
                default:
                    parsed = false;
                    break;

                case 3:
                    parsed &= byte.TryParse(hexSpan.Slice(0, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r);
                    parsed &= byte.TryParse(hexSpan.Slice(1, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g);
                    parsed &= byte.TryParse(hexSpan.Slice(2, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);

                    r *= 17;
                    g *= 17;
                    b *= 17;
                    break;

                case 6:
                    parsed &= byte.TryParse(hexSpan.Slice(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r);
                    parsed &= byte.TryParse(hexSpan.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g);
                    parsed &= byte.TryParse(hexSpan.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
                    break;

                case 4:
                    parsed &= byte.TryParse(hexSpan.Slice(0, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r);
                    parsed &= byte.TryParse(hexSpan.Slice(1, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g);
                    parsed &= byte.TryParse(hexSpan.Slice(2, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
                    parsed &= byte.TryParse(hexSpan.Slice(3, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a);

                    r *= 17;
                    g *= 17;
                    b *= 17;
                    a *= 17;
                    break;

                case 8:
                    parsed &= byte.TryParse(hexSpan.Slice(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r);
                    parsed &= byte.TryParse(hexSpan.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g);
                    parsed &= byte.TryParse(hexSpan.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
                    parsed &= byte.TryParse(hexSpan.Slice(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a);
                    break;
            }

            colour = new Colour4(r, g, b, a);
            return parsed;
        }

        /// <summary>
        /// Converts a <see cref="Colour4"/> into a hex colour code.
        /// </summary>
        /// <param name="alwaysOutputAlpha">Whether the alpha channel should always be output. If <c>false</c>, the alpha channel is only output if this colour is translucent.</param>
        /// <returns>The hex code representing the colour.</returns>
        public string ToHex(bool alwaysOutputAlpha = false)
        {
            uint argb = ToARGB();
            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)argb;

            if (!alwaysOutputAlpha && a == 255)
                return $"#{r:X2}{g:X2}{b:X2}";

            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        /// <summary>
        /// Converts an HSV colour to a <see cref="Colour4"/>. All components should be in the range 0-1.
        /// </summary>
        /// <param name="hue">The hue, between 0 and 1.</param>
        /// <param name="saturation">The saturation, between 0 and 1.</param>
        /// <param name="value">The value, between 0 and 1.</param>
        /// <param name="alpha">The alpha, between 0 and 1.</param>
        public static Colour4 FromHSV(float hue, float saturation, float value, float alpha = 1f)
        {
            int hi = (int)(hue * 6);
            float f = hue * 6f - hi;
            float p = value * (1 - saturation);
            float q = value * (1 - f * saturation);
            float t = value * (1 - (1 - f) * saturation);

            switch (hi)
            {
                default:
                case 0:
                    return new Colour4(value, t, p, alpha);

                case 1:
                    return new Colour4(q, value, p, alpha);

                case 2:
                    return new Colour4(p, value, t, alpha);

                case 3:
                    return new Colour4(p, q, value, alpha);

                case 4:
                    return new Colour4(t, p, value, alpha);

                case 5:
                    return new Colour4(value, p, q, alpha);
            }
        }

        /// <summary>
        /// Converts a <see cref="Colour4"/> to the HSV colour space, represented in the XYZ components of a <see cref="Vector4"/>.
        /// The returned vector's W component represents the alpha channel of the colour.
        /// The angular hue is compressed to the 0-1 range in line with the other components.
        /// </summary>
        /// <returns>A <see cref="Vector4"/> representing the colour, where all four components are in the 0-1 range.</returns>
        public Vector4 ToHSV()
        {
            float red = R;
            float green = G;
            float blue = B;

            float max = Math.Max(red, Math.Max(green, blue));
            float min = Math.Min(red, Math.Min(green, blue));

            float hue;

            if (max == min)
                hue = 0;
            else if (max == red)
                hue = (6f + (green - blue) / (max - min)) % 6f;
            else if (max == green)
                hue = (blue - red) / (max - min) + 2;
            else
                hue = (red - green) / (max - min) + 4;

            float saturation = max == 0 ? 0 : (max - min) / max;
            hue = Math.Clamp(hue / 6f, 0f, 1f);

            return new Vector4(hue == 1f ? 0f : hue, saturation, max, A);
        }

        /// <summary>
        /// Converts an HSL colour to a <see cref="Colour4"/>. All components should be in the range 0-1.
        /// </summary>
        /// <param name="hue">The hue, between 0 and 1.</param>
        /// <param name="saturation">The saturation, between 0 and 1.</param>
        /// <param name="lightness">The value, between 0 and 1.</param>
        /// <param name="alpha">The alpha, between 0 and 1.</param>
        public static Colour4 FromHSL(float hue, float saturation, float lightness, float alpha = 1f)
        {
            float c = (1f - Math.Abs(2f * lightness - 1f)) * saturation;
            float h = hue * 6f;
            float x = c * (1f - Math.Abs(h % 2f - 1f));

            float r, g, b;

            if (0f <= h && h < 1f)
            {
                r = c;
                g = x;
                b = 0f;
            }
            else if (1f <= h && h < 2f)
            {
                r = x;
                g = c;
                b = 0f;
            }
            else if (2f <= h && h < 3f)
            {
                r = 0f;
                g = c;
                b = x;
            }
            else if (3f <= h && h < 4f)
            {
                r = 0f;
                g = x;
                b = c;
            }
            else if (4f <= h && h < 5f)
            {
                r = x;
                g = 0f;
                b = c;
            }
            else if (5f <= h && h <= 6f)
            {
                r = c;
                g = 0f;
                b = x;
            }
            else
                r = g = b = 0f;

            float m = lightness - c * 0.5f;
            return new Colour4(r + m, g + m, b + m, alpha);
        }

        /// <summary>
        /// Converts a <see cref="Colour4"/> to the HSL colour space, represented in the XYZ components of a <see cref="Vector4"/>.
        /// The returned vector's W component represents the alpha channel of the colour.
        /// The angular hue is compressed to the 0-1 range in line with the other components.
        /// </summary>
        /// <returns>A <see cref="Vector4"/> representing the colour, where all four components are in the 0-1 range.</returns>
        public Vector4 ToHSL()
        {
            float red = R;
            float green = G;
            float blue = B;

            float max = Math.Max(red, Math.Max(green, blue));
            float min = Math.Min(red, Math.Min(green, blue));

            float lightness = (max + min) / 2f;
            if (lightness <= 0)
                return new Vector4(0f, 0f, 0f, A);

            float diff = max - min;
            float saturation = diff;
            if (saturation <= 0)
                return new Vector4(0f, 0f, lightness, A);

            saturation = lightness <= 0.5f ? saturation / (min + max) : saturation / (2f - max - min);

            float hue = 0f;

            if (max == red)
                hue = (green - blue) / diff;
            else if (max == green)
                hue = (blue - red) / diff + 2f;
            else if (max == blue)
                hue = (red - green) / diff + 4f;

            hue /= 6f;
            if (hue < 0f)
                hue += 1f;

            return new Vector4(hue, saturation, lightness, A);
        }

        private const double gamma = 2.4;

        private static double toLinear(double color) => color <= 0.04045 ? color / 12.92 : Math.Pow((color + 0.055) / 1.055, gamma);

        private static double toSRGB(double color) => color < 0.0031308 ? 12.92 * color : 1.055 * Math.Pow(color, 1.0 / gamma) - 0.055;

        #endregion

        #region Equality

        public bool Equals(Colour4 other) => Vector.Equals(other.Vector);

        public override bool Equals(object obj) => obj is Colour4 other && Equals(other);

        public override int GetHashCode() => Vector.GetHashCode();

        #endregion

        public override string ToString() => $"(R, G, B, A) = ({R:F}, {G:F}, {B:F}, {A:F})";

        #region Constants

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 0).
        /// </summary>
        public static Colour4 Transparent => new Colour4(255, 255, 255, 0);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 248, 255, 255).
        /// </summary>
        public static Colour4 AliceBlue => new Colour4(240, 248, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 235, 215, 255).
        /// </summary>
        public static Colour4 AntiqueWhite => new Colour4(250, 235, 215, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Colour4 Aqua => new Colour4(0, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 212, 255).
        /// </summary>
        public static Colour4 Aquamarine => new Colour4(127, 255, 212, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 255, 255).
        /// </summary>
        public static Colour4 Azure => new Colour4(240, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 220, 255).
        /// </summary>
        public static Colour4 Beige => new Colour4(245, 245, 220, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 196, 255).
        /// </summary>
        public static Colour4 Bisque => new Colour4(255, 228, 196, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 0, 255).
        /// </summary>
        public static Colour4 Black => new Colour4(0, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 235, 205, 255).
        /// </summary>
        public static Colour4 BlanchedAlmond => new Colour4(255, 235, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 255, 255).
        /// </summary>
        public static Colour4 Blue => new Colour4(0, 0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (138, 43, 226, 255).
        /// </summary>
        public static Colour4 BlueViolet => new Colour4(138, 43, 226, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (165, 42, 42, 255).
        /// </summary>
        public static Colour4 Brown => new Colour4(165, 42, 42, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (222, 184, 135, 255).
        /// </summary>
        public static Colour4 BurlyWood => new Colour4(222, 184, 135, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (95, 158, 160, 255).
        /// </summary>
        public static Colour4 CadetBlue => new Colour4(95, 158, 160, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (127, 255, 0, 255).
        /// </summary>
        public static Colour4 Chartreuse => new Colour4(127, 255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 105, 30, 255).
        /// </summary>
        public static Colour4 Chocolate => new Colour4(210, 105, 30, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 127, 80, 255).
        /// </summary>
        public static Colour4 Coral => new Colour4(255, 127, 80, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (100, 149, 237, 255).
        /// </summary>
        public static Colour4 CornflowerBlue => new Colour4(100, 149, 237, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 248, 220, 255).
        /// </summary>
        public static Colour4 Cornsilk => new Colour4(255, 248, 220, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 20, 60, 255).
        /// </summary>
        public static Colour4 Crimson => new Colour4(220, 20, 60, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Colour4 Cyan => new Colour4(0, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 139, 255).
        /// </summary>
        public static Colour4 DarkBlue => new Colour4(0, 0, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 139, 139, 255).
        /// </summary>
        public static Colour4 DarkCyan => new Colour4(0, 139, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (184, 134, 11, 255).
        /// </summary>
        public static Colour4 DarkGoldenrod => new Colour4(184, 134, 11, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (169, 169, 169, 255).
        /// </summary>
        public static Colour4 DarkGray => new Colour4(169, 169, 169, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 100, 0, 255).
        /// </summary>
        public static Colour4 DarkGreen => new Colour4(0, 100, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (189, 183, 107, 255).
        /// </summary>
        public static Colour4 DarkKhaki => new Colour4(189, 183, 107, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 139, 255).
        /// </summary>
        public static Colour4 DarkMagenta => new Colour4(139, 0, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (85, 107, 47, 255).
        /// </summary>
        public static Colour4 DarkOliveGreen => new Colour4(85, 107, 47, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 140, 0, 255).
        /// </summary>
        public static Colour4 DarkOrange => new Colour4(255, 140, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (153, 50, 204, 255).
        /// </summary>
        public static Colour4 DarkOrchid => new Colour4(153, 50, 204, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 0, 0, 255).
        /// </summary>
        public static Colour4 DarkRed => new Colour4(139, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (233, 150, 122, 255).
        /// </summary>
        public static Colour4 DarkSalmon => new Colour4(233, 150, 122, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (143, 188, 139, 255).
        /// </summary>
        public static Colour4 DarkSeaGreen => new Colour4(143, 188, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 61, 139, 255).
        /// </summary>
        public static Colour4 DarkSlateBlue => new Colour4(72, 61, 139, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (47, 79, 79, 255).
        /// </summary>
        public static Colour4 DarkSlateGray => new Colour4(47, 79, 79, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 206, 209, 255).
        /// </summary>
        public static Colour4 DarkTurquoise => new Colour4(0, 206, 209, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (148, 0, 211, 255).
        /// </summary>
        public static Colour4 DarkViolet => new Colour4(148, 0, 211, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 20, 147, 255).
        /// </summary>
        public static Colour4 DeepPink => new Colour4(255, 20, 147, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 191, 255, 255).
        /// </summary>
        public static Colour4 DeepSkyBlue => new Colour4(0, 191, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (105, 105, 105, 255).
        /// </summary>
        public static Colour4 DimGray => new Colour4(105, 105, 105, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (30, 144, 255, 255).
        /// </summary>
        public static Colour4 DodgerBlue => new Colour4(30, 144, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (178, 34, 34, 255).
        /// </summary>
        public static Colour4 Firebrick => new Colour4(178, 34, 34, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 240, 255).
        /// </summary>
        public static Colour4 FloralWhite => new Colour4(255, 250, 240, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (34, 139, 34, 255).
        /// </summary>
        public static Colour4 ForestGreen => new Colour4(34, 139, 34, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Colour4 Fuchsia => new Colour4(255, 0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (220, 220, 220, 255).
        /// </summary>
        public static Colour4 Gainsboro => new Colour4(220, 220, 220, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (248, 248, 255, 255).
        /// </summary>
        public static Colour4 GhostWhite => new Colour4(248, 248, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 215, 0, 255).
        /// </summary>
        public static Colour4 Gold => new Colour4(255, 215, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 165, 32, 255).
        /// </summary>
        public static Colour4 Goldenrod => new Colour4(218, 165, 32, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 128, 255).
        /// </summary>
        public static Colour4 Gray => new Colour4(128, 128, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 0, 255).
        /// </summary>
        public static Colour4 Green => new Colour4(0, 128, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 255, 47, 255).
        /// </summary>
        public static Colour4 GreenYellow => new Colour4(173, 255, 47, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 255, 240, 255).
        /// </summary>
        public static Colour4 Honeydew => new Colour4(240, 255, 240, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 105, 180, 255).
        /// </summary>
        public static Colour4 HotPink => new Colour4(255, 105, 180, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 92, 92, 255).
        /// </summary>
        public static Colour4 IndianRed => new Colour4(205, 92, 92, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (75, 0, 130, 255).
        /// </summary>
        public static Colour4 Indigo => new Colour4(75, 0, 130, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 240, 255).
        /// </summary>
        public static Colour4 Ivory => new Colour4(255, 255, 240, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 230, 140, 255).
        /// </summary>
        public static Colour4 Khaki => new Colour4(240, 230, 140, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (230, 230, 250, 255).
        /// </summary>
        public static Colour4 Lavender => new Colour4(230, 230, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 240, 245, 255).
        /// </summary>
        public static Colour4 LavenderBlush => new Colour4(255, 240, 245, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (124, 252, 0, 255).
        /// </summary>
        public static Colour4 LawnGreen => new Colour4(124, 252, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 205, 255).
        /// </summary>
        public static Colour4 LemonChiffon => new Colour4(255, 250, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (173, 216, 230, 255).
        /// </summary>
        public static Colour4 LightBlue => new Colour4(173, 216, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (240, 128, 128, 255).
        /// </summary>
        public static Colour4 LightCoral => new Colour4(240, 128, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (224, 255, 255, 255).
        /// </summary>
        public static Colour4 LightCyan => new Colour4(224, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 250, 210, 255).
        /// </summary>
        public static Colour4 LightGoldenrodYellow => new Colour4(250, 250, 210, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (144, 238, 144, 255).
        /// </summary>
        public static Colour4 LightGreen => new Colour4(144, 238, 144, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (211, 211, 211, 255).
        /// </summary>
        public static Colour4 LightGray => new Colour4(211, 211, 211, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 182, 193, 255).
        /// </summary>
        public static Colour4 LightPink => new Colour4(255, 182, 193, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 160, 122, 255).
        /// </summary>
        public static Colour4 LightSalmon => new Colour4(255, 160, 122, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (32, 178, 170, 255).
        /// </summary>
        public static Colour4 LightSeaGreen => new Colour4(32, 178, 170, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 250, 255).
        /// </summary>
        public static Colour4 LightSkyBlue => new Colour4(135, 206, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (119, 136, 153, 255).
        /// </summary>
        public static Colour4 LightSlateGray => new Colour4(119, 136, 153, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 196, 222, 255).
        /// </summary>
        public static Colour4 LightSteelBlue => new Colour4(176, 196, 222, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 224, 255).
        /// </summary>
        public static Colour4 LightYellow => new Colour4(255, 255, 224, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 0, 255).
        /// </summary>
        public static Colour4 Lime => new Colour4(0, 255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (50, 205, 50, 255).
        /// </summary>
        public static Colour4 LimeGreen => new Colour4(50, 205, 50, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 240, 230, 255).
        /// </summary>
        public static Colour4 Linen => new Colour4(250, 240, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Colour4 Magenta => new Colour4(255, 0, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 0, 255).
        /// </summary>
        public static Colour4 Maroon => new Colour4(128, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (102, 205, 170, 255).
        /// </summary>
        public static Colour4 MediumAquamarine => new Colour4(102, 205, 170, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 205, 255).
        /// </summary>
        public static Colour4 MediumBlue => new Colour4(0, 0, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (186, 85, 211, 255).
        /// </summary>
        public static Colour4 MediumOrchid => new Colour4(186, 85, 211, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (147, 112, 219, 255).
        /// </summary>
        public static Colour4 MediumPurple => new Colour4(147, 112, 219, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (60, 179, 113, 255).
        /// </summary>
        public static Colour4 MediumSeaGreen => new Colour4(60, 179, 113, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (123, 104, 238, 255).
        /// </summary>
        public static Colour4 MediumSlateBlue => new Colour4(123, 104, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 250, 154, 255).
        /// </summary>
        public static Colour4 MediumSpringGreen => new Colour4(0, 250, 154, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (72, 209, 204, 255).
        /// </summary>
        public static Colour4 MediumTurquoise => new Colour4(72, 209, 204, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (199, 21, 133, 255).
        /// </summary>
        public static Colour4 MediumVioletRed => new Colour4(199, 21, 133, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (25, 25, 112, 255).
        /// </summary>
        public static Colour4 MidnightBlue => new Colour4(25, 25, 112, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 255, 250, 255).
        /// </summary>
        public static Colour4 MintCream => new Colour4(245, 255, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 225, 255).
        /// </summary>
        public static Colour4 MistyRose => new Colour4(255, 228, 225, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 228, 181, 255).
        /// </summary>
        public static Colour4 Moccasin => new Colour4(255, 228, 181, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 222, 173, 255).
        /// </summary>
        public static Colour4 NavajoWhite => new Colour4(255, 222, 173, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
        /// </summary>
        public static Colour4 Navy => new Colour4(0, 0, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (253, 245, 230, 255).
        /// </summary>
        public static Colour4 OldLace => new Colour4(253, 245, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 128, 0, 255).
        /// </summary>
        public static Colour4 Olive => new Colour4(128, 128, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (107, 142, 35, 255).
        /// </summary>
        public static Colour4 OliveDrab => new Colour4(107, 142, 35, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 165, 0, 255).
        /// </summary>
        public static Colour4 Orange => new Colour4(255, 165, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 69, 0, 255).
        /// </summary>
        public static Colour4 OrangeRed => new Colour4(255, 69, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (218, 112, 214, 255).
        /// </summary>
        public static Colour4 Orchid => new Colour4(218, 112, 214, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 232, 170, 255).
        /// </summary>
        public static Colour4 PaleGoldenrod => new Colour4(238, 232, 170, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (152, 251, 152, 255).
        /// </summary>
        public static Colour4 PaleGreen => new Colour4(152, 251, 152, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (175, 238, 238, 255).
        /// </summary>
        public static Colour4 PaleTurquoise => new Colour4(175, 238, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (219, 112, 147, 255).
        /// </summary>
        public static Colour4 PaleVioletRed => new Colour4(219, 112, 147, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 239, 213, 255).
        /// </summary>
        public static Colour4 PapayaWhip => new Colour4(255, 239, 213, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 218, 185, 255).
        /// </summary>
        public static Colour4 PeachPuff => new Colour4(255, 218, 185, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (205, 133, 63, 255).
        /// </summary>
        public static Colour4 Peru => new Colour4(205, 133, 63, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 192, 203, 255).
        /// </summary>
        public static Colour4 Pink => new Colour4(255, 192, 203, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (221, 160, 221, 255).
        /// </summary>
        public static Colour4 Plum => new Colour4(221, 160, 221, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (176, 224, 230, 255).
        /// </summary>
        public static Colour4 PowderBlue => new Colour4(176, 224, 230, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (128, 0, 128, 255).
        /// </summary>
        public static Colour4 Purple => new Colour4(128, 0, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 0, 0, 255).
        /// </summary>
        public static Colour4 Red => new Colour4(255, 0, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (188, 143, 143, 255).
        /// </summary>
        public static Colour4 RosyBrown => new Colour4(188, 143, 143, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (65, 105, 225, 255).
        /// </summary>
        public static Colour4 RoyalBlue => new Colour4(65, 105, 225, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (139, 69, 19, 255).
        /// </summary>
        public static Colour4 SaddleBrown => new Colour4(139, 69, 19, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (250, 128, 114, 255).
        /// </summary>
        public static Colour4 Salmon => new Colour4(250, 128, 114, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (244, 164, 96, 255).
        /// </summary>
        public static Colour4 SandyBrown => new Colour4(244, 164, 96, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (46, 139, 87, 255).
        /// </summary>
        public static Colour4 SeaGreen => new Colour4(46, 139, 87, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 245, 238, 255).
        /// </summary>
        public static Colour4 SeaShell => new Colour4(255, 245, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (160, 82, 45, 255).
        /// </summary>
        public static Colour4 Sienna => new Colour4(160, 82, 45, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (192, 192, 192, 255).
        /// </summary>
        public static Colour4 Silver => new Colour4(192, 192, 192, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (135, 206, 235, 255).
        /// </summary>
        public static Colour4 SkyBlue => new Colour4(135, 206, 235, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (106, 90, 205, 255).
        /// </summary>
        public static Colour4 SlateBlue => new Colour4(106, 90, 205, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (112, 128, 144, 255).
        /// </summary>
        public static Colour4 SlateGray => new Colour4(112, 128, 144, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 250, 250, 255).
        /// </summary>
        public static Colour4 Snow => new Colour4(255, 250, 250, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 255, 127, 255).
        /// </summary>
        public static Colour4 SpringGreen => new Colour4(0, 255, 127, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (70, 130, 180, 255).
        /// </summary>
        public static Colour4 SteelBlue => new Colour4(70, 130, 180, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (210, 180, 140, 255).
        /// </summary>
        public static Colour4 Tan => new Colour4(210, 180, 140, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (0, 128, 128, 255).
        /// </summary>
        public static Colour4 Teal => new Colour4(0, 128, 128, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (216, 191, 216, 255).
        /// </summary>
        public static Colour4 Thistle => new Colour4(216, 191, 216, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 99, 71, 255).
        /// </summary>
        public static Colour4 Tomato => new Colour4(255, 99, 71, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (64, 224, 208, 255).
        /// </summary>
        public static Colour4 Turquoise => new Colour4(64, 224, 208, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (238, 130, 238, 255).
        /// </summary>
        public static Colour4 Violet => new Colour4(238, 130, 238, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 222, 179, 255).
        /// </summary>
        public static Colour4 Wheat => new Colour4(245, 222, 179, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 255, 255).
        /// </summary>
        public static Colour4 White => new Colour4(255, 255, 255, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (245, 245, 245, 255).
        /// </summary>
        public static Colour4 WhiteSmoke => new Colour4(245, 245, 245, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (255, 255, 0, 255).
        /// </summary>
        public static Colour4 Yellow => new Colour4(255, 255, 0, 255);

        /// <summary>
        /// Gets the system color with (R, G, B, A) = (154, 205, 50, 255).
        /// </summary>
        public static Colour4 YellowGreen => new Colour4(154, 205, 50, 255);

        #endregion
    }
}
