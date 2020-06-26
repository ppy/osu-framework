// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Foundation;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;
using UIKit;

namespace osu.Framework.iOS.Input
{
    public class IOSTouchHandler : InputHandler
    {
        private readonly IOSGameView view;

        private UIEventButtonMask? lastButtonMask;

        private readonly bool indirectPointerSupported = UIDevice.CurrentDevice.CheckSystemVersion(13, 4);

        public IOSTouchHandler(IOSGameView view)
        {
            this.view = view;
            view.HandleTouches += handleTouches;
        }

        private void handleTouches(NSSet obj, UIEvent evt)
        {
            foreach (var t in obj)
                handleUITouch((UITouch)t, evt);
        }

        private void handleUITouch(UITouch touch, UIEvent e)
        {
            // always update position.
            var location = touch.LocationInView(null);

            PendingInputs.Enqueue(new MousePositionAbsoluteInput
            {
                Position = new Vector2((float)location.X * view.Scale, (float)location.Y * view.Scale)
            });

            if (indirectPointerSupported && touch.Type == UITouchType.IndirectPointer)
            {
                // Indirect pointer means the touch came from a mouse cursor, and wasn't a physical touch on the screen
                switch (touch.Phase)
                {
                    case UITouchPhase.Began:
                    case UITouchPhase.Moved:
                        // only one button can be in a "down" state at once. all previous buttons are automatically released.
                        // we need to handle this assumption at our end.
                        if (lastButtonMask != null && lastButtonMask != e.ButtonMask)
                            PendingInputs.Enqueue(new MouseButtonInput(buttonFromMask(lastButtonMask.Value), false));

                        PendingInputs.Enqueue(new MouseButtonInput(buttonFromMask(e.ButtonMask), true));
                        lastButtonMask = e.ButtonMask;
                        break;

                    case UITouchPhase.Cancelled:
                    case UITouchPhase.Ended:
                        Debug.Assert(lastButtonMask != null);

                        PendingInputs.Enqueue(new MouseButtonInput(buttonFromMask(lastButtonMask.Value), false));
                        lastButtonMask = null;
                        break;
                }
            }
            else
            {
                // simple logic before multiple button support was introduced.
                // TODO: going forward, this should also handle multi-touch input.
                switch (touch.Phase)
                {
                    case UITouchPhase.Began:
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));
                        break;

                    case UITouchPhase.Cancelled:
                    case UITouchPhase.Ended:
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, false));
                        break;
                }
            }
        }

        private MouseButton buttonFromMask(UIEventButtonMask buttonMask)
        {
            Debug.Assert(indirectPointerSupported);

            switch (buttonMask)
            {
                default:
                    return MouseButton.Left;

                case UIEventButtonMask.Secondary:
                    return MouseButton.Right;
            }
        }

        protected override void Dispose(bool disposing)
        {
            view.HandleTouches -= handleTouches;
            base.Dispose(disposing);
        }

        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host) => true;
    }
}
