﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input
{
    internal class FrameworkActionContainer : KeyBindingContainer<FrameworkAction>
    {
        public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
        {
            new KeyBinding(new[] { InputKey.Control, InputKey.F1 }, FrameworkAction.ToggleDrawVisualiser),
            new KeyBinding(new[] { InputKey.Control, InputKey.F2 }, FrameworkAction.ToggleGlobalStatistics),
            new KeyBinding(new[] { InputKey.Control, InputKey.F11 }, FrameworkAction.CycleFrameStatistics),
            new KeyBinding(new[] { InputKey.Control, InputKey.F10 }, FrameworkAction.ToggleLogOverlay),
            new KeyBinding(new[] { InputKey.Alt, InputKey.Enter }, FrameworkAction.ToggleFullscreen),
            new KeyBinding(new[] { InputKey.F11 }, FrameworkAction.ToggleFullscreen)
        };

        protected override bool Prioritised => true;
    }

    public enum FrameworkAction
    {
        CycleFrameStatistics,
        ToggleDrawVisualiser,
        ToggleGlobalStatistics,
        ToggleLogOverlay,
        ToggleFullscreen
    }
}
