// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Platform
{
    public enum AudioBackend
    {
        [Description("Automatic")]
        Automatic,

        [Description("BASS")]
        Bass,

        [Description("BASSWASAPI")]
        BassWasapi,
    }
}
