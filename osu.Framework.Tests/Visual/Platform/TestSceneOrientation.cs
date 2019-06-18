// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneOrientation : FrameworkTestScene
    {
        private readonly BasicDropdown<WindowOrientationMode> dropdown;
        private Bindable<WindowOrientationMode> orientation;

        public TestSceneOrientation()
        {
            AddRange(new Drawable[]
            {
                dropdown = new BasicDropdown<WindowOrientationMode>()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 150,
                }
            });
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host, FrameworkConfigManager config)
        {
            if (host.Window == null) return;

            orientation = config.GetBindable<WindowOrientationMode>(FrameworkSetting.WindowOrientationMode);
            orientation.BindTo(dropdown.Current);

            foreach (var orintation in host.Window.SupportedOrientationModes)
                dropdown.AddDropdownItem(orintation);
        }
    }
}
