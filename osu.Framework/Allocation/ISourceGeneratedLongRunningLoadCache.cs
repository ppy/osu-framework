// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Allocation
{
    public interface ISourceGeneratedLongRunningLoadCache
    {
        protected internal Type KnownType { get; }
        protected internal bool IsLongRunning { get; }
    }
}
