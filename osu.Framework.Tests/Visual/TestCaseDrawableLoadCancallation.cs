// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class TestCaseDrawableLoadCancallation : TestCase
    {
        private readonly List<SlowLoader> loaders = new List<SlowLoader>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("add slow loader", () =>
            {
                loaders.Clear();
                Child = createLoader();
            });

            AddStep("replace slow loader", () => { Child = createLoader(); });
            AddStep("replace slow loader", () => { Child = createLoader(); });
            AddStep("replace slow loader", () => { Child = createLoader(); });

            AddUntilStep(() => loaders.AsEnumerable().Reverse().Skip(1).All(l => l.WasCancelled), "all but last loader cancelled");

            AddAssert("last loader not cancelled", () => !loaders.Last().WasCancelled);

            AddUntilStep(() => loaders.Last().HasLoaded, "last loader loaded");
        }

        private int id;

        private SlowLoader createLoader()
        {
            var loader = new SlowLoader(id++);
            loaders.Add(loader);
            return loader;
        }

        public class SlowLoader : CompositeDrawable
        {
            private readonly int id;
            private SlowLoadDrawable loadable;

            public bool WasCancelled => loadable?.WasCancelled ?? false;
            public bool HasLoaded => loadable?.IsLoaded ?? false;

            public SlowLoader(int id)
            {
                this.id = id;
                RelativeSizeAxes = Axes.Both;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                Size = new Vector2(0.9f);

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Navy,
                        RelativeSizeAxes = Axes.Both,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                this.FadeInFromZero(200);
                LoadComponentAsync(loadable = new SlowLoadDrawable(id), AddInternal);
            }
        }

        public class SlowLoadDrawable : CompositeDrawable
        {
            private readonly int id;

            public bool WasCancelled;

            public SlowLoadDrawable(int id)
            {
                this.id = id;

                RelativeSizeAxes = Axes.Both;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                Size = new Vector2(0.9f);

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
                        Colour = Color4.Black,
                        TextSize = 50,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(CancellationToken? cancellation)
            {
                int i = Math.Max(1, (int)(100 / Clock.Rate));

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

            protected override void LoadComplete()
            {
                base.LoadComplete();
                this.FadeInFromZero(200);
            }
        }
    }
}
