// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Testing.Drawables.Sections
{
    public class ToolbarAssemblySection : ToolbarSection
    {
        private AssemblyDropdown assemblyDropdown;

        public ToolbarAssemblySection()
        {
            AutoSizeAxes = Axes.X;
            Masking = false;
        }

        [BackgroundDependencyLoader]
        private void load(TestBrowser browser)
        {
            InternalChild = new FillFlowContainer
            {
                Spacing = new Vector2(5),
                Direction = FillDirection.Horizontal,
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Padding = new MarginPadding(5),
                        Font = FrameworkFont.Condensed,
                        Text = "Assembly"
                    },
                    assemblyDropdown = new AssemblyDropdown
                    {
                        Width = 250,
                        Current = browser.Assembly
                    },
                    new BasicCheckbox
                    {
                        LabelText = "Run all steps",
                        RightHandedCheckbox = true,
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Current = browser.RunAllSteps
                    },
                }
            };
        }

        public void AddAssembly(string name, Assembly assembly) => assemblyDropdown.AddAssembly(name, assembly);

        private class AssemblyDropdown : BasicDropdown<Assembly>
        {
            public void AddAssembly(string name, Assembly assembly)
            {
                if (assembly == null) return;

                foreach (var item in MenuItems.ToArray())
                {
                    if (item.Text.Value.ToString().Contains("dynamic"))
                        RemoveDropdownItem(item.Value);
                }

                AddDropdownItem(name, assembly);
            }
        }
    }
}
