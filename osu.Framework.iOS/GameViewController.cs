// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using CoreGraphics;
using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS
{
    internal class GameViewController : UIViewController
    {
        private readonly IOSGameView gameView;
        private readonly GameHost gameHost;

        public override bool PrefersStatusBarHidden() => true;

        public override bool PrefersHomeIndicatorAutoHidden => true;

        public override UIRectEdge PreferredScreenEdgesDeferringSystemGestures => UIRectEdge.All;

        public GameViewController(IOSGameView view, GameHost host)
        {
            View = view;

            gameView = view;
            gameHost = host;
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            gameHost.Collect();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            coordinator.AnimateAlongsideTransition(_ => { }, _ => UIView.AnimationsEnabled = true);
            UIView.AnimationsEnabled = false;

            base.ViewWillTransitionToSize(toSize, coordinator);
            gameView.RequestResizeFrameBuffer();
        }
    }
}
