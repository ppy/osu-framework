// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneDrawableLoadCancellation : FrameworkTestScene
    {
        private readonly List<SlowLoader> loaders = new List<SlowLoader>();

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            loaders.Clear();
            Child = createLoader();
        });

        [Test]
        public void TestConcurrentLoad()
        {
            AddStep("replace slow loader", () => { Child = createLoader(); });
            AddStep("replace slow loader", () => { Child = createLoader(); });
            AddStep("replace slow loader", () => { Child = createLoader(); });

            AddUntilStep("all but last loader cancelled", () => loaders.AsEnumerable().Reverse().Skip(1).All(l => l.WasCancelled));

            AddUntilStep("last loader began loading", () => !loaders.Last().WasCancelled);

            AddStep("allow load to complete", () => loaders.Last().AllowLoadCompletion());

            AddUntilStep("last loader loaded", () => loaders.Last().HasLoaded);
        }

        [Test]
        public void TestLoadAsyncCancel()
        {
            bool loaded = false;

            PausableLoadDrawable loader = null;
            CancellationTokenSource cancellationSource = null;

            AddStep("start async load", () => LoadComponentAsync(loader = new PausableLoadDrawable(0), _ => loaded = true, (cancellationSource = new CancellationTokenSource()).Token));

            AddUntilStep("load started", () => loader.IsLoading);

            AddStep("cancel", () => cancellationSource.Cancel());

            AddUntilStep("load cancelled", () => !loader.IsLoading);
            AddAssert("didn't callback", () => !loaded);
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
            private PausableLoadDrawable loadable;

            public bool WasCancelled => loadable?.IsLoading == false;
            public bool HasLoaded => loadable?.IsLoaded ?? false;

            public void AllowLoadCompletion() => loadable?.AllowLoadCompletion();

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
                LoadComponentAsync(loadable = new PausableLoadDrawable(id), AddInternal);
            }
        }

        public class PausableLoadDrawable : CompositeDrawable
        {
            private readonly int id;

            public bool IsLoading;

            public PausableLoadDrawable(int id)
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
                        Font = new FontUsage(size: 50),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                };
            }

            private readonly CancellationTokenSource ourSource = new CancellationTokenSource();

            [BackgroundDependencyLoader]
            private void load(CancellationToken? cancellation)
            {
                IsLoading = true;

                using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(ourSource.Token, cancellation ?? CancellationToken.None))
                {
                    try
                    {
                        Task.Delay(99999, linkedSource.Token).Wait(linkedSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        if (!ourSource.IsCancellationRequested)
                        {
                            IsLoading = false;
                            throw;
                        }
                    }
                }

                Logger.Log($"Load {id} complete!");
            }

            public void AllowLoadCompletion() => ourSource.Cancel();

            protected override void LoadComplete()
            {
                base.LoadComplete();
                this.FadeInFromZero(200);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                ourSource.Dispose();
            }
        }
    }
}
