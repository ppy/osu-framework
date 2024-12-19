// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.iOS;
using osu.Framework.Tests.Visual;
using UIKit;

namespace osu.Framework.Tests.iOS
{
    public partial class TestSceneLockScreenOrientation : FrameworkTestScene
    {
        [Test]
        public void TestToggle()
        {
            AddToggleStep("lock orientation", v =>
            {
                var appDelegate = (GameApplicationDelegate)UIApplication.SharedApplication.Delegate;
                appDelegate.LockScreenOrientation = v;
            });
        }
    }
}
