// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Input
{
    public class UserInputManager : PassThroughInputManager
    {
        protected override ImmutableArray<InputHandler> InputHandlers => Host.AvailableInputHandlers;

        public override bool HandleHoverEvents => Host.Window?.CursorInWindow.Value ?? true;

        protected internal override bool ShouldBeAlive => true;

        protected internal UserInputManager()
        {
            // UserInputManager is at the very top of the draw hierarchy, so it has no parent updating its IsAlive state
            IsAlive = true;
            UseParentInput = false;
        }

        public override void HandleInputStateChange(InputStateChangeEvent inputStateChange)
        {
            switch (inputStateChange)
            {
                case MousePositionChangeEvent mousePositionChange:
                    var mouse = mousePositionChange.State.Mouse;

                    // confine cursor
                    if (Host.Window != null)
                    {
                        RectangleF? cursorConfineRect = null;
                        var clientSize = Host.Window.ClientSize;
                        var windowRect = new RectangleF(0, 0, clientSize.Width, clientSize.Height);

                        if (Host.Window.CursorState.HasFlagFast(CursorState.Confined))
                        {
                            cursorConfineRect = Host.Window.CursorConfineRect ?? windowRect;
                        }
                        else if (mouseOutsideAllDisplays(mouse.Position))
                        {
                            // Implicitly confine the cursor to prevent a feedback loop of MouseHandler warping the cursor to an invalid position
                            // and the OS immediately warping it back inside a display.

                            // Window.CursorConfineRect is not used here as that should only be used when confining is explicitly enabled.
                            cursorConfineRect = windowRect;
                        }

                        if (cursorConfineRect.HasValue)
                            mouse.Position = Vector2.Clamp(mouse.Position, cursorConfineRect.Value.Location, cursorConfineRect.Value.Location + cursorConfineRect.Value.Size - Vector2.One);
                    }

                    break;

                case ButtonStateChangeEvent<MouseButton> buttonChange:
                    if (buttonChange.Kind == ButtonStateChangeKind.Pressed && Host.Window?.CursorInWindow.Value == false)
                        return;

                    break;

                case MouseScrollChangeEvent:
                    if (Host.Window?.CursorInWindow.Value == false)
                        return;

                    break;
            }

            base.HandleInputStateChange(inputStateChange);
        }

        private bool mouseOutsideAllDisplays(Vector2 mousePosition)
        {
            Point windowLocation;

            switch (Host.Window.WindowMode.Value)
            {
                case WindowMode.Windowed:
                    windowLocation = Host.Window is SDL2DesktopWindow sdlWindow ? sdlWindow.Position : Point.Empty;
                    break;

                default:
                    windowLocation = Host.Window.CurrentDisplayBindable.Value.Bounds.Location;
                    break;
            }

            int x = (int)MathF.Floor(windowLocation.X + mousePosition.X);
            int y = (int)MathF.Floor(windowLocation.Y + mousePosition.Y);

            return !Host.Window.Displays.Any(d => d.Bounds.Contains(x, y));
        }
    }
}
