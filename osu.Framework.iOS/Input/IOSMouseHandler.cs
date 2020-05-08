// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Foundation;
using ObjCRuntime;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS.Input
{
    public class IOSMouseHandler : InputHandler, IUIPointerInteractionDelegate
    {
        private readonly IOSGameView view;
        private readonly UIPointerInteraction pointerInteraction;

        public IOSMouseHandler(IOSGameView view)
        {
            // UIPointerInteraction is only available on iOS 13.4 and up
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
                return;

            this.view = view;
            view.AddInteraction(pointerInteraction = new UIPointerInteraction(this));
        }

        public override bool IsActive => (pointerInteraction != null);

        public override int Priority => 1; // Touches always take priority

        public IntPtr Handle => throw new NotImplementedException();

        public override bool Initialize(GameHost host) => true;

        [Export("pointerInteraction:styleForRegion:")]
        public UIPointerStyle GetStyleForRegion(UIPointerInteraction interaction, UIPointerRegion region)
        {
            return UIPointerStyle.CreateHiddenPointerStyle();
        }
    }
}