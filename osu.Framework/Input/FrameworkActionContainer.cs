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

        protected override IEnumerable<Drawable> KeyBindingInputQueue => new[] { Child }.Concat(base.KeyBindingInputQueue);
    }
}