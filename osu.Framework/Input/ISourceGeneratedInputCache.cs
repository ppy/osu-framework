// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Input
{
    public interface ISourceGeneratedInputCache
    {
        Type KnownType { get; }
        bool RequestsPositionalInput { get; }
        bool RequestsNonPositionalInput { get; }
    }
}
