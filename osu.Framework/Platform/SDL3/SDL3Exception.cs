// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    internal class SDL3Exception(string? expression) : Exception($"{SDL_GetError()} (at {expression})");
}
