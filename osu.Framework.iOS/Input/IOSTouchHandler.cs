// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Handlers;
using Foundation;
using UIKit;
using osu.Framework.Platform;
using osu.Framework.Input.StateChanges;
using osuTK;
using osuTK.Input;

namespace osu.Framework.iOS.Input
{
    public class IOSTouchHandler : InputHandler
    {
        private readonly IOSGameView view;
        private NSMutableSet<UITouch> pendingRightClickTouches = new NSMutableSet<UITouch>();

        private bool rightClickSupport = UIDevice.CurrentDevice.CheckSystemVersion(13, 4);

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
            // Indirect pointer means the touch came from a mouse cursor, and wasn't a physcial touch on the screen
            bool indirectTouch = (rightClickSupport && touch.Type == UITouchType.IndirectPointer);
            bool rightClickEvent = (rightClickSupport && evt.ButtonMask == UIEventButtonMask.Secondary);

            var location = touch.LocationInView(null);
            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = new Vector2((float)location.X * view.Scale, (float)location.Y * view.Scale) });

            switch (touch.Phase)
            {
                case UITouchPhase.Began:
                    if (indirectTouch && rightClickEvent)
                    {
                        pendingRightClickTouches.Add(touch);
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, true));
                    }
                    else
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));

                    break;
                case UITouchPhase.Moved:
                    if (!indirectTouch)
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));
                    else
                    {
                        // A single UITouch object represents the mouse cursor on iPadOS 13.4.
                        // If the user clicks both left and right buttons on a physical mouse, this doesn't generate more
                        // touch objects; it just changes the button mask value for the one touch object without calling "Began" or "Ended".
                        // Without accounting for this, the mouse button input can sometimes be left in a "stuck" state.

                        if (rightClickEvent)
                        {
                            // If a right-click event occurred, and the touch wasn't already saved in the right-click set,
                            // the user has transitioned from left-click to right-click.
                            if (!pendingRightClickTouches.Contains(touch))
                            {
                                PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, false));
                                pendingRightClickTouches.Add(touch);
                            }

                            PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, true));
                        }
                        else
                        {
                            // If a left-click event has occurred, but the touch event was already saved in the right-click set,
                            // the user has transitioned from a right-click event to a left-click.
                            if (pendingRightClickTouches.Contains(touch))
                            {
                                PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, false));
                                pendingRightClickTouches.Remove(touch);
                            }

                            PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));
                        }
                    }

                    break;
                case UITouchPhase.Cancelled:
                case UITouchPhase.Ended:
                    if (indirectTouch && pendingRightClickTouches.Contains(touch))
                    {
                        pendingRightClickTouches.Remove(touch);
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, false));
                    }
                    else
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, false));
                    break;
            }
        }

        public override bool IsActive => true;

        public override int Priority => 0;

        protected override void Dispose(bool disposing)
        {
            view.HandleTouches -= handleTouches;
            base.Dispose(disposing);
        }

        public override bool Initialize(GameHost host) => true;
    }
}
