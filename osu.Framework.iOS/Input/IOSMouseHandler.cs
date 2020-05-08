// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
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
        private readonly UIPointerInteraction pointerInteraction;
        private readonly IOSMouseDelegate mouseDelegate;

        public IOSMouseHandler(IOSGameView view)
        {
            // UIPointerInteraction is only available on iOS 13.4 and up
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                return;

            this.view = view;
            view.AddInteraction(pointerInteraction = new UIPointerInteraction(mouseDelegate = new IOSMouseDelegate()));
            mouseDelegate.LocationUpdated += locationUpdated;
        }

        public override bool IsActive => (pointerInteraction != null);

        public override int Priority => 1; // Touches always take priority

        public override bool Initialize(GameHost host) => true;

        private void locationUpdated(CGPoint location)
        {
            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = new Vector2((float)location.X * view.Scale,
                (float)location.Y * view.Scale) });
        }
    }

    public class IOSMouseDelegate: NSObject, IUIPointerInteractionDelegate
    {
        public Action<CGPoint> LocationUpdated;

        [Export("pointerInteraction:regionForRequest:defaultRegion:")]
        public UIPointerRegion GetRegionForRequest(UIPointerInteraction interaction, UIPointerRegionRequest request, UIPointerRegion defaultRegion)
        {
            LocationUpdated(request.Location);
            return defaultRegion;
        }

        [Export("pointerInteraction:styleForRegion:")]
        public UIPointerStyle GetStyleForRegion(UIPointerInteraction interaction, UIPointerRegion region)
        {
            return UIPointerStyle.CreateHiddenPointerStyle();
        }
    }
}