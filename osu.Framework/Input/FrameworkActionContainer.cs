// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input
{
    public class FrameworkActionContainer : KeyBindingContainer<FrameworkAction>
    {
        public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
        {
            new KeyBinding(new[] { InputKey.Control, InputKey.F1 }, FrameworkAction.ToggleDrawVisualiser),
            new KeyBinding(new[] { InputKey.Control, InputKey.F11 }, FrameworkAction.CycleFrameStatistics),
            new KeyBinding(new[] { InputKey.Control, InputKey.F10 }, FrameworkAction.ToggleLogOverlay),
            new KeyBinding(new[] { InputKey.Alt, InputKey.Enter }, FrameworkAction.ToggleFullscreen),
        };

        protected override bool Prioritised => true;

        protected override IEnumerable<Drawable> KeyBindingInputQueue => base.KeyBindingInputQueue.Prepend(Children.First());
    }

    public enum FrameworkAction
    {
        CycleFrameStatistics,
        ToggleDrawVisualiser,
        ToggleLogOverlay,
        ToggleFullscreen
    }
}
