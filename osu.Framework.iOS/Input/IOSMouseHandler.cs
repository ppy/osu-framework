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

        public IOSMouseHandler(IOSGameView view)
        {
            this.view = view;
        }

        public override bool IsActive => true;

        public override int Priority => 1; // Touches always take priority

        [UsedImplicitly]
        private IOSMouseDelegate mouseDelegate;

        public override bool Initialize(GameHost host)
        {
            // UIPointerInteraction is only available on iOS 13.4 and up
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                return false;

            view.AddInteraction(new UIPointerInteraction(mouseDelegate = new IOSMouseDelegate()));

            mouseDelegate.LocationUpdated += locationUpdated;
            return true;
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
