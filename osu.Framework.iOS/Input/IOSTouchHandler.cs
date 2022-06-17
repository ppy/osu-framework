// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
using Foundation;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
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

        private readonly UITouch[] activeTouches = new UITouch[TouchState.MAX_TOUCH_COUNT];

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
            var cgLocation = touch.LocationInView(null);
            Vector2 location = new Vector2((float)cgLocation.X * view.Scale, (float)cgLocation.Y * view.Scale);

            if (indirectPointerSupported && touch.Type == UITouchType.IndirectPointer)
                handleIndirectPointer(touch, e.ButtonMask, location);
            else
                handleTouch(touch, location);
        }

        private void handleIndirectPointer(UITouch touch, UIEventButtonMask buttonMask, Vector2 location)
        {
            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = location });

            // Indirect pointer means the touch came from a mouse cursor, and wasn't a physical touch on the screen
            switch (touch.Phase)
            {
                case UITouchPhase.Began:
                case UITouchPhase.Moved:
                    // only one button can be in a "down" state at once. all previous buttons are automatically released.
                    // we need to handle this assumption at our end.
                    if (lastButtonMask != null && lastButtonMask != buttonMask)
                        PendingInputs.Enqueue(new MouseButtonInput(buttonFromMask(lastButtonMask.Value), false));

                    PendingInputs.Enqueue(new MouseButtonInput(buttonFromMask(buttonMask), true));
                    lastButtonMask = buttonMask;
                    break;

                case UITouchPhase.Cancelled:
                case UITouchPhase.Ended:
                    Debug.Assert(lastButtonMask != null);

                    PendingInputs.Enqueue(new MouseButtonInput(buttonFromMask(lastButtonMask.Value), false));
                    lastButtonMask = null;
                    break;
            }
        }

        private void handleTouch(UITouch uiTouch, Vector2 location)
        {
            TouchSource? existingSource = getTouchSource(uiTouch);

            if (uiTouch.Phase == UITouchPhase.Began)
            {
                // need to assign the new touch.
                Debug.Assert(existingSource == null);

                existingSource = assignNextAvailableTouchSource(uiTouch);
            }

            if (existingSource == null)
                return;

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

        private TouchSource? assignNextAvailableTouchSource(UITouch uiTouch)
        {
            for (int i = 0; i < activeTouches.Length; i++)
            {
                if (activeTouches[i] != null) continue;

                activeTouches[i] = uiTouch;
                return (TouchSource)i;
            }

            // we only handle up to TouchState.MAX_TOUCH_COUNT. Ignore any further touches for now.
            return null;
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

        public override bool Initialize(GameHost host) => true;
    }
}
