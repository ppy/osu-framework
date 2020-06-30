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
            var location = touch.LocationInView(null);

            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = new Vector2((float)location.X * view.Scale, (float)location.Y * view.Scale) });

            switch (touch.Phase)
            {
                case UITouchPhase.Moved:
                case UITouchPhase.Began:
                    if (rightClickSupport && evt.ButtonMask == UIEventButtonMask.Secondary)
                    {
                        pendingRightClickTouches.Add(touch);
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, true));
                    }
                    else
                        PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));

                    break;

                case UITouchPhase.Cancelled:
                case UITouchPhase.Ended:
                    if (pendingRightClickTouches.Contains(touch))
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
