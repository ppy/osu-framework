// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicColourPicker : ColourPicker
    {
        protected override HSVColourPicker CreateHSVColourPicker() => new BasicHSVColourPicker();
        protected override HexColourPicker CreateHexColourPicker() => new BasicHexColourPicker();
    }
}
