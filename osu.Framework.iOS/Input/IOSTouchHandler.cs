// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Foundation;
using osu.Framework.Input;
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
        private const int max_touches = 10; // should match TouchSource enum count.

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

        private UITouch[] activeTouches = new UITouch[10];

        private void handleUITouch(UITouch uiTouch, UIEvent e)
        {
            // always update position.
            var cgLocation = uiTouch.LocationInView(null);
            Vector2 location = new Vector2((float)cgLocation.X * view.Scale, (float)cgLocation.Y * view.Scale);

            if (indirectPointerSupported && uiTouch.Type == UITouchType.IndirectPointer)
            {
                PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = location });

                // Indirect pointer means the touch came from a mouse cursor, and wasn't a physical touch on the screen
                switch (uiTouch.Phase)
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
                TouchSource? existingSource = getTouchSource(uiTouch);

                if (uiTouch.Phase == UITouchPhase.Began)
                {
                    // need to assign the new touch.
                    Debug.Assert(existingSource == null);

                    for (int i = 0; i < activeTouches.Length; i++)
                    {
                        if (activeTouches[i] != null) continue;

                        activeTouches[i] = uiTouch;
                        existingSource = (TouchSource)i;
                        break;
                    }
                }

                Debug.Assert(existingSource != null);

                var touch = new Touch(existingSource.Value, location);

                // standard touch handling
                switch (uiTouch.Phase)
                {
                    case UITouchPhase.Began:
                    case UITouchPhase.Moved:
                        PendingInputs.Enqueue(new TouchInput(touch, true));
                        break;

                    case UITouchPhase.Cancelled:
                    case UITouchPhase.Ended:
                        PendingInputs.Enqueue(new TouchInput(touch, false));

                        // touch no longer valid, remove from reference array.
                        activeTouches[(int)existingSource] = null;
                        break;
                }
            }
        }

        private TouchSource? getTouchSource(UITouch touch)
        {
            for (int i = 0; i < activeTouches.Length; i++)
            {
                // The recommended (and only) way to track touches is storing and comparing references of the UITouch objects.
                // https://stackoverflow.com/questions/39823914/how-to-track-multiple-touches
                if (ReferenceEquals(activeTouches[i], touch))
                    return (TouchSource)i;
            }

            return null;
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
