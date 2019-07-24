// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using CoreGraphics;
using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS
{
    internal class GameViewController : UIViewController
    {
        private readonly IOSGameView view;
        private readonly GameHost host;

        public override bool PrefersStatusBarHidden() => true;

        public override UIRectEdge PreferredScreenEdgesDeferringSystemGestures => UIRectEdge.All;

        public GameViewController(IOSGameView view, GameHost host)
        {
            View = view;
            this.host = host;
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            host.Collect();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            coordinator.AnimateAlongsideTransition(_ => { }, _ => UIView.AnimationsEnabled = true);
            UIView.AnimationsEnabled = false;
            base.ViewWillTransitionToSize(toSize, coordinator);

            view.RequestResizeFrameBuffer();
        }
    }
}
