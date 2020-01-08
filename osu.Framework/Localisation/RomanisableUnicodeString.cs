// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string that has variation in Unicode and Romanised form.
    /// </summary>
    public class RomanisableUnicodeString
    {
        public readonly string Romanised;
        public readonly string Unicode;

        public RomanisableUnicodeString(string romanised, string unicode)
        {
            Romanised = romanised;
            Unicode = unicode;
        }

        public string GetPreferred(bool preferUnicode)
        {
            if (string.IsNullOrEmpty(Romanised)) return Unicode;
            if (string.IsNullOrEmpty(Unicode)) return Romanised;

            return preferUnicode ? Unicode : Romanised;
        }

        public override string ToString() => GetPreferred(false);
    }
}
