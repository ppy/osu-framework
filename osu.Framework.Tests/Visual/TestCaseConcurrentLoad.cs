// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseConcurrentLoad : TestCase
    {
        private SlowFlow load1;
        private SlowFlow load2;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("add slow flow", () =>
            {
                Child = load1 = new SlowFlow
                {
                    Direction = FillDirection.Full,
                    RelativeSizeAxes = Axes.Both,
                    Spacing = new Vector2(5)
                };
            });

            AddStep("replace low flow", () =>
            {
                Child = load2 = new SlowFlow
                {
                    Direction = FillDirection.Full,
                    RelativeSizeAxes = Axes.Both,
                    Spacing = new Vector2(5)
                };
            });

            AddAssert("first load is cancelled", () => load1.WasCancelled);
            AddAssert("second load not cancelled", () => !load2.WasCancelled);

            AddUntilStep(() => load2.HasLoaded, "second load complete");
        }

        public class SlowFlow : FillFlowContainer
        {
            private SlowLoadDrawable loadable;

            public bool WasCancelled => loadable?.WasCancelled ?? false;
            public bool HasLoaded => loadable?.IsLoaded ?? false;

            protected override void LoadComplete()
            {
                base.LoadComplete();
                LoadComponentAsync(loadable = new SlowLoadDrawable(0), Add);
            }
        }

        public class SlowLoadDrawable : CompositeDrawable
        {
            private readonly int id;

            public bool WasCancelled;

            public SlowLoadDrawable(int id)
            {
                this.id = id;
                Size = new Vector2(50);
                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.NavajoWhite,
                        RelativeSizeAxes = Axes.Both
                    },
                    new SpriteText
                    {
                        Text = id.ToString(),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(CancellationToken? cancellation)
            {
                int i = 100;

                while (i-- > 0)
                {
                    Thread.Sleep(10);
                    if (cancellation?.IsCancellationRequested == true)
                    {
                        WasCancelled = true;
                        return;
                    }
                }

                //await Task.Delay(10000, cancellation ?? CancellationToken.None);
                Logger.Log($"Load {id} complete!");
            }
        }
    }
}
