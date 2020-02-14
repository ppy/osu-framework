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

        public IOSTouchHandler(IOSGameView view)
        {
            this.view = view;
            view.HandleTouches += handleTouches;
        }

        private void handleTouches(NSSet obj)
        {
            if (obj.Count == 1)
                handleUITouch((UITouch)obj.AnyObject);
            else
            {
                foreach (var t in obj)
                    handleUITouch((UITouch)t);
            }
        }

        private void handleUITouch(UITouch touch)
        {
            var location = touch.LocationInView(null);

            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = new Vector2((float)location.X * view.Scale, (float)location.Y * view.Scale) });

            switch (touch.Phase)
            {
                case UITouchPhase.Moved:
                case UITouchPhase.Began:
                    PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Left, true));
                    break;

                case UITouchPhase.Cancelled:
                case UITouchPhase.Ended:
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
