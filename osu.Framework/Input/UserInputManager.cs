// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class UserInputManager : KeyBindingInputManager<FrameworkAction>
    {
        protected override IEnumerable<InputHandler> InputHandlers => Host.AvailableInputHandlers;

        protected override IEnumerable<KeyBinding> CreateDefaultMappings() => new[]
        {
            new KeyBinding(new[] { Key.LControl, Key.F1 }, FrameworkAction.ToggleDrawVisualiser),
            new KeyBinding(new[] { Key.LControl, Key.F11 }, FrameworkAction.CycleFrameStatistics),
            new KeyBinding(new[] { Key.LControl, Key.F10 }, FrameworkAction.ToggleLogOverlay),
            new KeyBinding(new[] { Key.LAlt, Key.Enter }, FrameworkAction.ToggleFullscreen),
        };

        public UserInputManager()
        {
            UseParentState = false;
        }

        protected override bool PropagateKeyDown(IEnumerable<Drawable> drawables, InputState state, KeyDownEventArgs args)
        {
            // always handle ourselves before all children.
            return base.PropagateKeyDown(new[] { Child }.Concat(drawables), state, args);
        }

        protected override bool PropagateKeyUp(IEnumerable<Drawable> drawables, InputState state, KeyUpEventArgs args)
        {
            // always handle ourselves before all children.
            return base.PropagateKeyUp(new[] { Child }.Concat(drawables), state, args);
        }
    }

    public enum FrameworkAction
    {
        CycleFrameStatistics,
        ToggleDrawVisualiser,
        ToggleLogOverlay,
        ToggleFullscreen
    }
}
