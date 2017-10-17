// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;

namespace osu.Framework.Input
{
    public class UserInputManager : KeyBindingInputManager<FrameworkAction>
    {
        private readonly Drawable handler;

        protected override IEnumerable<InputHandler> InputHandlers => Host.AvailableInputHandlers;

        public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
        {
            new KeyBinding(new[] { InputKey.Control, InputKey.F1 }, FrameworkAction.ToggleDrawVisualiser),
            new KeyBinding(new[] { InputKey.Control, InputKey.F11 }, FrameworkAction.CycleFrameStatistics),
            new KeyBinding(new[] { InputKey.Control, InputKey.F10 }, FrameworkAction.ToggleLogOverlay),
            new KeyBinding(new[] { InputKey.Alt, InputKey.Enter }, FrameworkAction.ToggleFullscreen),
        };

        public UserInputManager(Drawable handler = null)
        {
            this.handler = handler;
            UseParentState = false;
        }

        protected override IEnumerable<Drawable> KeyBindingInputQueue => new[] { handler ?? Child }.Concat(base.KeyBindingInputQueue);
    }

    public enum FrameworkAction
    {
        CycleFrameStatistics,
        ToggleDrawVisualiser,
        ToggleLogOverlay,
        ToggleFullscreen
    }
}
