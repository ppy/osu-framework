// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using OpenTK;

namespace osu.Framework.Testing.Drawables.Sections
{
    public class ToolbarAssemblySection : ToolbarSection
    {
        private readonly Bindable<Assembly> assembly = new Bindable<Assembly>();
        private readonly Bindable<bool> runAllSteps = new Bindable<bool>();

        private BasicDropdown<Assembly> assemblyDropdown;

        public ToolbarAssemblySection()
        {
            AutoSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load(TestBrowser.AssemblyBindable assembly, TestBrowser.RunAllStepsBindable runAllSteps)
        {
            this.assembly.BindTo(assembly);
            this.runAllSteps.BindTo(runAllSteps);

            BasicCheckbox runAllStepsCheckbox;

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
                        Text = "Assembly:"
                    },
                    assemblyDropdown = new BasicDropdown<Assembly>
                    {
                        Width = 250,
                    },
                    runAllStepsCheckbox = new BasicCheckbox
                    {
                        LabelText = "Run all steps",
                        LabelPadding = new MarginPadding { Left = 5, Right = 10 },
                        AutoSizeAxes = Axes.Y,
                        Width = 140,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                }
            };

            assemblyDropdown.Current.BindTo(this.assembly);
            runAllStepsCheckbox.Current.BindTo(this.runAllSteps);
        }

        public void AddAssembly(string name, Assembly assembly)
        {
            const string dynamic_assembly_identifier = "dynamic";
            assemblyDropdown.RemoveDropdownItem(assemblyDropdown.Items.LastOrDefault(i => i.Key.Contains(dynamic_assembly_identifier)).Value);
            assemblyDropdown.AddDropdownItem(name, assembly);
        }
    }
}
