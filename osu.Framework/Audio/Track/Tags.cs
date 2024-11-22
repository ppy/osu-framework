// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// A container for audio track metadata.
    /// </summary>
    public class Tags
    {
        public string? Title { get; set; }

        public string? Artist { get; set; }

        public string? Album { get; set; }

        public int? Year { get; set; }

        public string? Genre { get; set; }
    }
}
