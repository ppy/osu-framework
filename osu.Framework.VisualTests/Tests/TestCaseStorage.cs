using System;
using OpenTK;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
	public class TestCaseStorage : TestCase
	{
		private BasicStorage storage;

		public override string Name => @"BasicStorage";
		public override string Description => @"Storage and watch capabilities";

		public override void Reset()
		{
			base.Reset();
			updateContainer();
		}

		[BackgroundDependencyLoader]
		private void load(BasicGameHost host)
		{
			storage = host.Storage;
			storage.SetupWatcher("");
			storage.OnChanged = updateContainer;
		}

		private void updateContainer()
		{
			Clear();
			Add(new FlowContainer
			{
				AutoSizeAxes = Axes.Y,
				RelativeSizeAxes = Axes.X,
				Direction = FlowDirections.Vertical,

				Children = new Drawable[] {
					new SpriteText()
					{
						Text = "In the osu folder:",
						TextSize = 40,
						Margin = new MarginPadding { Right = 10, Top = 10 },
					},
					new SpriteText()
					{
						Text = "Foo.txt " + (storage.Exists("Foo.txt") ? "exists" : "does not exist"),
						TextSize = 20,
						Margin = new MarginPadding { Right = 10, Top = 10 },
					},
					new SpriteText()
					{
						Text = "Bar.ini " + (storage.Exists("Bar.ini") ? "exists" : "does not exist"),
						TextSize = 20,
						Margin = new MarginPadding { Right = 10, Top = 10 },
					},
					new SpriteText()
					{
						Text = "Baz.png " + (storage.Exists("Baz.png") ? "exists" : "does not exist"),
						TextSize = 20,
						Margin = new MarginPadding { Right = 10, Top = 10 },
					},
				},
			});
		}
	}
}
