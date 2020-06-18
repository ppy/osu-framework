// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    public class IOSMouseHandler : InputHandler
    {
        private readonly IOSGameView view;
        private UIPointerInteraction pointerInteraction;
        private UIPanGestureRecognizer panGestureRecognizer;
        private CGPoint previousPanTranslation;

        [UsedImplicitly]
        private IOSMouseDelegate mouseDelegate;

        public IOSMouseHandler(IOSGameView view)
        {
            this.view = view;
        }

        public override bool IsActive => true;
        public override int Priority => 1; // Touches always take priority

        public override bool Initialize(GameHost host)
        {
            // UIPointerInteraction is only available on iOS 13.4 and up
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                return false;

            pointerInteraction = new UIPointerInteraction(mouseDelegate = new IOSMouseDelegate());
            mouseDelegate.LocationUpdated += locationUpdated;
            view.AddInteraction(pointerInteraction);

            panGestureRecognizer = new UIPanGestureRecognizer(panOffsetUpdated)
            {
                AllowedScrollTypesMask = UIScrollTypeMask.Continuous,
                MaximumNumberOfTouches = 0 // Only enables this for 2 finger swipe, disabling clicking and dragging.
            };
            view.AddGestureRecognizer(panGestureRecognizer);
            
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            view.RemoveInteraction(pointerInteraction);
            view.RemoveGestureRecognizer(panGestureRecognizer);
            base.Dispose(disposing);
        }

        private void locationUpdated(CGPoint location)
        {
            PendingInputs.Enqueue(new MousePositionAbsoluteInput
            {
                Position = new Vector2(
                    (float)location.X * view.Scale,
                    (float)location.Y * view.Scale)
            });
        }

        private void panOffsetUpdated()
        {
            CGPoint translation = panGestureRecognizer.TranslationInView(view);
            if (panGestureRecognizer.State == UIGestureRecognizerState.Began)
                previousPanTranslation = translation;

            CGPoint delta = new CGPoint(
                    translation.X - previousPanTranslation.X,
                    translation.Y - previousPanTranslation.Y
                );

            PendingInputs.Enqueue(new MouseScrollRelativeInput
            {
                IsPrecise = true,
                Delta = new Vector2((float)delta.X, (float)delta.Y)
            });

            previousPanTranslation = translation;
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
