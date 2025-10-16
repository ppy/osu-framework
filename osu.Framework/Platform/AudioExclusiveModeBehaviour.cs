// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Platform
{
    public enum AudioExclusiveModeBehaviour
    {
        [Description("Never")]
        Never,

        [Description("Always")]
        Always,

        [Description("During Active")]
        DuringActive,
    }
}
