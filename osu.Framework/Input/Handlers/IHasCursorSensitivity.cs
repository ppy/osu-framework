﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Configuration;

namespace osu.Framework.Input.Handlers
{
    /// <summary>
    /// An input handler which can have its sensitivity changed.
    /// </summary>
    public interface IHasCursorSensitivity
    {
        BindableDouble Sensitivity { get; }
    }
}
