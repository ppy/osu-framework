// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        private UIEventButtonMask lastButtonMask = UIEventButtonMask.Primary;

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

        private void handleUITouch(UITouch touch, UIEvent evt)
        {
            var location = touch.LocationInView(null);

            // Indirect pointer means the touch came from a mouse cursor, and wasn't a physical touch on the screen
            bool isIndirect = (indirectPointerSupported && touch.Type == UITouchType.IndirectPointer);

            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = new Vector2((float)location.X * view.Scale, (float)location.Y * view.Scale) });

            switch (touch.Phase)
            {
                case UITouchPhase.Began:
                    if (isIndirect)
                    {
                        lastButtonMask = evt.ButtonMask;
                        MouseButton mouseButton = isRightClick(lastButtonMask) ? MouseButton.Right : MouseButton.Left;
                        PendingInputs.Enqueue(new MouseButtonInput(mouseButton, true));
                    }
                    else
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));

                    break;

                case UITouchPhase.Moved:
                    if (isIndirect)
                        transitionRightClick(evt);
                    else
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));

                    break;

                case UITouchPhase.Cancelled:
                case UITouchPhase.Ended:
                    if (isIndirect)
                    {
                        MouseButton mouseButton = isRightClick(lastButtonMask) ? MouseButton.Right : MouseButton.Left;
                        PendingInputs.Enqueue(new MouseButtonInput(mouseButton, false));
                    }
                    else
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, false));

                    break;
            }
        }

        private void transitionRightClick(UIEvent evt)
        {
            if (!indirectPointerSupported)
                return;

            MouseButton activeButton = isRightClick(evt.ButtonMask) ? MouseButton.Right : MouseButton.Left;
            MouseButton inactiveButton = activeButton == MouseButton.Right ? MouseButton.Left : MouseButton.Right;

            // A single UITouch object represents the mouse cursor on iPadOS 13.4.
            // If the user clicks both left and right buttons on a physical mouse, this doesn't generate more
            // touch objects; it just changes the button mask value for the one touch object without calling "Began" or "Ended".
            // If the stored mask value doesn't match the active one, this means the user alternated buttons, so unclick
            // the previous button, and click the new button
            if (lastButtonMask != evt.ButtonMask)
            {
                PendingInputs.Enqueue(new MouseButtonInput(inactiveButton, false));
                lastButtonMask = evt.ButtonMask;
            }

            PendingInputs.Enqueue(new MouseButtonInput(activeButton, true));
        }

        private bool isRightClick(UIEventButtonMask buttonMask)
        {
            if (!indirectPointerSupported)
                return false;

            return (buttonMask == UIEventButtonMask.Secondary);
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
