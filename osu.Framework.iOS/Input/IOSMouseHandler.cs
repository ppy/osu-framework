// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using CoreGraphics;
using Foundation;
using JetBrains.Annotations;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK;
using UIKit;

namespace osu.Framework.iOS.Input
{
    /// <summary>
    /// Handles scroll and positional updates for external cursor-based input devices.
    /// Click / touch handling is still provided by <see cref="IOSTouchHandler"/>.
    /// </summary>
    public class IOSMouseHandler : InputHandler
    {
        private readonly IOSGameView view;
        private UIPointerInteraction pointerInteraction;
        private UIPanGestureRecognizer trackpadScrollRecognizer;
        private UIPanGestureRecognizer mouseScrollRecognizer;
        private CGPoint lastScrollTranslation;

        [UsedImplicitly]
        private IOSMouseDelegate mouseDelegate;

        public IOSMouseHandler(IOSGameView view)
        {
            this.view = view;
        }

        public override bool IsActive => true;

        public override bool Initialize(GameHost host)
        {
            // UIPointerInteraction is only available on iOS 13.4 and up
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                return false;

            view.AddInteraction(pointerInteraction = new UIPointerInteraction(mouseDelegate = new IOSMouseDelegate()));
            mouseDelegate.LocationUpdated += locationUpdated;

            view.AddGestureRecognizer(trackpadScrollRecognizer = new UIPanGestureRecognizer(() => panGestureRecognized(trackpadScrollRecognizer, true))
            {
                AllowedScrollTypesMask = UIScrollTypeMask.Continuous,
                MaximumNumberOfTouches = 0 // Only handle drags when no "touches" are active (ie. no buttons are in a pressed state)
            });

            view.AddGestureRecognizer(mouseScrollRecognizer = new UIPanGestureRecognizer(() => panGestureRecognized(mouseScrollRecognizer, false))
            {
                AllowedScrollTypesMask = UIScrollTypeMask.Discrete,
            });

            return true;
        }

        private void locationUpdated(CGPoint location)
        {
            PendingInputs.Enqueue(new MousePositionAbsoluteInput
            {
                Position = new Vector2((float)location.X * view.Scale, (float)location.Y * view.Scale)
            });
        }

        private const float scroll_rate_adjust = 0.1f;

        private void panGestureRecognized(UIPanGestureRecognizer recognizer, bool precise)
        {
            CGPoint translation = recognizer.TranslationInView(view);
            Vector2 delta;

            switch (recognizer.State)
            {
                case UIGestureRecognizerState.Began:
                    // consume initial value.
                    delta = new Vector2((float)translation.X, (float)translation.Y);
                    break;

                default:
                    // only consider relative change from previous value.
                    delta = new Vector2((float)(translation.X - lastScrollTranslation.X), (float)(translation.Y - lastScrollTranslation.Y));
                    break;
            }

            lastScrollTranslation = translation;

            PendingInputs.Enqueue(new MouseScrollRelativeInput
            {
                IsPrecise = true,
                Delta = delta * (precise ? scroll_rate_adjust : 1)
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (pointerInteraction != null)
                view.RemoveInteraction(pointerInteraction);

            if (trackpadScrollRecognizer != null)
                view.RemoveGestureRecognizer(trackpadScrollRecognizer);

            if (mouseScrollRecognizer != null)
                view.RemoveGestureRecognizer(mouseScrollRecognizer);
        }
    }

    public class IOSMouseDelegate : NSObject, IUIPointerInteractionDelegate
    {
        public Action<CGPoint> LocationUpdated;

        [Export("pointerInteraction:regionForRequest:defaultRegion:")]
        public UIPointerRegion GetRegionForRequest(UIPointerInteraction interaction, UIPointerRegionRequest request, UIPointerRegion defaultRegion)
        {
            LocationUpdated(request.Location);
            return defaultRegion;
        }

        [Export("pointerInteraction:styleForRegion:")]
        public UIPointerStyle GetStyleForRegion(UIPointerInteraction interaction, UIPointerRegion region) =>
            UIPointerStyle.CreateHiddenPointerStyle();
    }
}
