// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.ComponentModel;

namespace osu.Framework.Platform
{
    public enum ExecutionMode
    {
        [Description("Single thread")]
        SingleThread,

        [Description("Multithreaded")]
        MultiThreaded
    }
}
