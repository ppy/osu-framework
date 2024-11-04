// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input
{
    /// <summary>
    /// Represents a number of properties to consider during a text input session.
    /// </summary>
    /// <param name="Type">The type of text being inputted.</param>
    /// <param name="AllowIme">
    /// <para>
    /// Whether IME should be allowed during this text input session.
    /// Useful for situations where IME input is not wanted, such as for passwords, numbers, or romanised text.
    /// </para>
    /// <para>
    /// Note that this is just a hint to the native implementation, some might respect this,
    /// while others will ignore and always have the IME (dis)allowed.
    /// </para>
    /// </param>
    public record struct TextInputProperties(TextInputType Type, bool AllowIme);
}
