// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneAllowSuspension : FrameworkTestScene
    {
        private readonly Bindable<bool> allowSuspension = new Bindable<bool>(true);

        public TestSceneAllowSuspension()
        {
            Child = new SuspensionVisualiser { RelativeSizeAxes = Axes.Both };
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            host.AllowScreenSuspension.AddSource(allowSuspension);
        }

        [Test]
        public void TestToggleSuspension()
        {
            AddToggleStep("toggle allow suspension", v => allowSuspension.Value = v);
        }

        private class SuspensionVisualiser : Box
        {
            private readonly IBindable<bool> allowSuspension = new Bindable<bool>();

            [BackgroundDependencyLoader]
            private void load(GameHost host)
            {
                allowSuspension.BindTo(host.AllowScreenSuspension.Result);
                allowSuspension.BindValueChanged(v => Colour = v.NewValue ? Color4.Green : Color4.Red, true);
            }
        }
    }
}
