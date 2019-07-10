﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using CoreGraphics;
using UIKit;

namespace osu.Framework.iOS
{
    internal class GameViewController : UIViewController
    {
        public override bool PrefersStatusBarHidden() => true;

        public override UIRectEdge PreferredScreenEdgesDeferringSystemGestures => UIRectEdge.All;

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();

            SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
            GC.Collect();
        }

        public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
        {
            coordinator.AnimateAlongsideTransition(_ => { }, _ => UIView.AnimationsEnabled = true);
            UIView.AnimationsEnabled = false;
            base.ViewWillTransitionToSize(toSize, coordinator);

            (View as IOSGameView)?.RequestResizeFrameBuffer();
        }
    }
}
